using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class CompProperties_SolarFlareShield : CompProperties
{
    // Room temperature the shield heats towards during a flare.
    public float targetTemperature = 65f;
    public float heatPerSecond = 200f;

    public CompProperties_SolarFlareShield()
    {
        compClass = typeof(CompSolarFlareShield);
    }
}

// Keeps its room powered through a solar flare. Draws its full consumption during a flare and nothing otherwise.
public sealed class CompSolarFlareShield : ThingComp
{
    private const int HeatInterval = 60;

    private CompPowerTrader? power;

    public CompProperties_SolarFlareShield Props => (CompProperties_SolarFlareShield)props;

    private bool RoomEnclosed
    {
        get
        {
            Room? room = parent.GetRoom();
            return room != null && !room.UsesOutdoorTemperature;
        }
    }

    public bool ShieldingRoom => power != null && power.PowerOn && RoomEnclosed;

    public bool WaitingToShield => power != null && !power.PowerOn && RoomEnclosed && FlickUtility.WantsToBeOn(parent) && !parent.IsBrokenDown();

    private bool Engaged => parent.Map.gameConditionManager.ElectricityDisabled(parent.Map) && RTSolarFlareShieldCompat.ActiveShield(parent.Map) == null;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
        parent.Map.GetComponent<MapComponent_SolarFlareShields>().shields.Add(this);
        ApplyDraw();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        map.GetComponent<MapComponent_SolarFlareShields>().shields.Remove(this);
    }

    public override void CompTick()
    {
        base.CompTick();
        ApplyDraw();
        if (parent.IsHashIntervalTick(HeatInterval) && Engaged && ShieldingRoom)
        {
            PushHeat(parent.GetRoom());
        }
    }

    public override string? CompInspectStringExtra()
    {
        if (!parent.Spawned)
        {
            return null;
        }
        return RTSolarFlareShieldCompat.InterferenceMessage(parent.Map) ?? (RoomEnclosed ? null : "BOR_SolarFlareShield_NotShielding".Translate());
    }

    // Vanilla resets the draw to the building's default in places, so this runs again on the tick.
    private void ApplyDraw()
    {
        if (power != null)
        {
            power.PowerOutput = Engaged ? -power.Props.PowerConsumption : 0f;
        }
    }

    // Capped by what the room still needs, so a small room does not jump past the target.
    private void PushHeat(Room room)
    {
        float heat = Mathf.Min(Props.heatPerSecond, (Props.targetTemperature - room.Temperature) * room.CellCount);
        if (heat > 0f)
        {
            room.PushHeat(heat);
        }
    }
}

// A shield only works in a room that does not count as outdoors, which vanilla's indoor check allows.
public class PlaceWorker_SolarFlareShield : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing? thingToIgnore = null, Thing? thing = null)
    {
        Room? room = loc.GetRoom(map);
        if (room == null || room.UsesOutdoorTemperature)
        {
            return "BOR_SolarFlareShield_NeedsRoom".Translate();
        }
        return AcceptanceReport.WasAccepted;
    }
}

public class MapComponent_SolarFlareShields : MapComponent
{
    public readonly List<CompSolarFlareShield> shields = new();

    public MapComponent_SolarFlareShields(Map map) : base(map)
    {
    }
}

// RT Solar Flare Shield lifts the flare from the whole map while one of its shields has power, so ours stand down.
public static class RTSolarFlareShieldCompat
{
    private const string ShieldDefName = "Building_RTMagneticShield";

    private static ThingDef? shieldDef;
    private static bool shieldDefLooked;

    // Looked up once: the flare patch asks on every power net tick.
    private static ThingDef? ShieldDef
    {
        get
        {
            if (!shieldDefLooked)
            {
                shieldDef = DefDatabase<ThingDef>.GetNamedSilentFail(ShieldDefName);
                shieldDefLooked = true;
            }
            return shieldDef;
        }
    }

    public static Thing? ActiveShield(Map map)
    {
        ThingDef? def = ShieldDef;
        if (def == null)
        {
            return null;
        }
        List<Thing> shields = map.listerThings.ThingsOfDef(def);
        for (int i = 0; i < shields.Count; i++)
        {
            if (shields[i].TryGetComp<CompPowerTrader>()?.PowerOn ?? true)
            {
                return shields[i];
            }
        }
        return null;
    }

    public static string? InterferenceMessage(Map map)
    {
        if (!map.gameConditionManager.ElectricityDisabled(map))
        {
            return null;
        }
        Thing? shield = ActiveShield(map);
        return shield == null ? null : "BOR_SolarFlareShield_Interference".Translate(shield.def.label);
    }
}

// A flare stops a power net as a whole. On a net with protected buildings, this runs the vanilla tick as if there
// were no flare but with only those buildings listed, and shuts down the rest the way a flare would.
[HarmonyPatch(typeof(PowerNet), nameof(PowerNet.PowerNetTick))]
public static class PowerNet_PowerNetTick_Patch
{
    private const int ShutdownInterval = 20;
    private const float ShutdownFraction = 0.05f;

    public static bool ignoreFlare;

    private static readonly HashSet<Room> shieldedRooms = new();
    private static readonly HashSet<Room> waitingRooms = new();
    private static readonly HashSet<Thing> shieldBuildings = new();
    private static readonly List<(IntVec3 center, float radius, bool up)> domes = new();
    private static readonly List<CompPowerTrader> protectedParts = new();
    private static readonly List<CompPowerTrader> exposedConsumers = new();

    // A map can have many nets; the shields only need working out once per map per tick.
    private static int shieldsTick = -1;
    private static int shieldsMapId = -1;

    public static void Prefix(PowerNet __instance, out List<CompPowerTrader>? __state)
    {
        __state = null;
        Map map = __instance.Map;
        if (!map.gameConditionManager.ElectricityDisabled(map) || RTSolarFlareShieldCompat.ActiveShield(map) != null)
        {
            return;
        }

        int tick = Find.TickManager.TicksGame;
        if (tick != shieldsTick || map.uniqueID != shieldsMapId)
        {
            shieldsTick = tick;
            shieldsMapId = map.uniqueID;
            CollectShields(map);
        }

        // With nothing that could protect a building, vanilla's flare handling is all this net needs.
        if (shieldBuildings.Count == 0)
        {
            return;
        }

        protectedParts.Clear();
        exposedConsumers.Clear();
        foreach (CompPowerTrader part in __instance.powerComps)
        {
            // A shield stays listed while off, so vanilla can power it back on. Until then its room or dome adds only
            // its generators, which may be all that can restart it; the consumers there wait for the shield.
            Room? room = part.parent.GetRoom();
            IntVec3 cell = part.parent.Position;
            bool inShieldedRoom = room != null && shieldedRooms.Contains(room);
            bool startsShield = part.PowerOutput > 0f && ((room != null && waitingRooms.Contains(room)) || InDome(cell, upOnly: false));
            if (inShieldedRoom || startsShield || shieldBuildings.Contains(part.parent) || InDome(cell, upOnly: true))
            {
                protectedParts.Add(part);
            }
            else if (part.PowerOn && part.EnergyOutputPerTick < 0f)
            {
                exposedConsumers.Add(part);
            }
        }

        if (protectedParts.Count == 0)
        {
            return;
        }

        if (exposedConsumers.Count > 0 && Find.TickManager.TicksGame % ShutdownInterval == 0)
        {
            int count = Mathf.Max(1, Mathf.RoundToInt(exposedConsumers.Count * ShutdownFraction));
            for (int i = 0; i < count; i++)
            {
                exposedConsumers.RandomElement().PowerOn = false;
            }
        }

        __state = __instance.powerComps;
        __instance.powerComps = protectedParts;
        ignoreFlare = true;
    }

    public static void Finalizer(PowerNet __instance, List<CompPowerTrader>? __state)
    {
        if (__state != null)
        {
            __instance.powerComps = __state;
            ignoreFlare = false;
        }
    }

    // Every shield building on the map, the rooms of the flare shields, and the domes with their reach: Umbral domes and
    // Orientis meditation shields. A dome is up when vanilla's interceptor says so, so whatever takes it down also ends
    // its flare protection.
    private static void CollectShields(Map map)
    {
        shieldedRooms.Clear();
        waitingRooms.Clear();
        shieldBuildings.Clear();
        domes.Clear();
        foreach (CompSolarFlareShield shield in map.GetComponent<MapComponent_SolarFlareShields>().shields)
        {
            shieldBuildings.Add(shield.parent);
            if (shield.ShieldingRoom)
            {
                shieldedRooms.Add(shield.parent.GetRoom());
            }
            else if (shield.WaitingToShield)
            {
                waitingRooms.Add(shield.parent.GetRoom());
            }
        }
        foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor))
        {
            CompProjectileInterceptor interceptor = thing.TryGetComp<CompProjectileInterceptor>();
            if (thing.TryGetComp<CompUmbralShield>() != null)
            {
                shieldBuildings.Add(thing);
                domes.Add((thing.Position, interceptor.Props.radius, interceptor.Active));
            }
            // Runs on meditation power, so one that is down has nothing to restart and counts for nothing.
            else if (thing.TryGetComp<CompMeditationShield>() != null && interceptor.Active)
            {
                shieldBuildings.Add(thing);
                domes.Add((thing.Position, interceptor.Props.radius, true));
            }
        }
    }

    // The same reach as CompUmbralShield.Covers, against this tick's domes. Umbral domes that are down count too unless
    // upOnly: their generators may be all that can power them back up during a flare.
    private static bool InDome(IntVec3 cell, bool upOnly)
    {
        foreach ((IntVec3 center, float radius, bool up) in domes)
        {
            if ((up || !upOnly) && cell.InHorDistOf(center, radius))
            {
                return true;
            }
        }
        return false;
    }
}

[HarmonyPatch(typeof(GameConditionManager), nameof(GameConditionManager.ElectricityDisabled))]
public static class GameConditionManager_ElectricityDisabled_Patch
{
    public static bool Prefix(ref bool __result)
    {
        if (PowerNet_PowerNetTick_Patch.ignoreFlare)
        {
            __result = false;
            return false;
        }
        return true;
    }
}
