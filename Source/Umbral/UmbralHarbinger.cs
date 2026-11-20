using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// The Harbinger that brings the Umbral herald: lands, lets the herald out, and leaves at once.
public class Building_HeraldHarbinger : Building
{
    private const int ReleaseDelayTicks = 60;
    private const int ReleaseRadius = 6;

    private Pawn? herald;
    private Pawn? target;
    private int ticksUntilRelease = ReleaseDelayTicks;
    private bool departed;

    public static void Send(Pawn herald, Pawn target, IntVec3 cell, Map map)
    {
        Building_HeraldHarbinger ship = (Building_HeraldHarbinger)ThingMaker.MakeThing(BOR_UmbralDefOf.BOR_HeraldHarbinger);
        ship.herald = herald;
        ship.target = target;
        SkyfallerMaker.SpawnSkyfaller(BOR_UmbralDefOf.BOR_HeraldHarbingerIncoming, ship, cell, map);
    }

    public override AcceptanceReport DeconstructibleBy(Faction faction) => false;

    public override AcceptanceReport ClaimableBy(Faction by) => false;

    protected override void Tick()
    {
        base.Tick();
        if (departed || !Spawned)
        {
            return;
        }
        if (herald != null)
        {
            if (--ticksUntilRelease > 0)
            {
                return;
            }
            Release();
        }
        Depart();
    }

    private void Release()
    {
        Pawn pawn = herald!;
        herald = null;
        CellRect hull = this.OccupiedRect();
        if (!CellFinder.TryFindRandomCellNear(Position, Map, ReleaseRadius, c => c.Standable(Map) && !hull.Contains(c), out IntVec3 cell))
        {
            cell = hull.ExpandedBy(1).EdgeCells.FirstOrDefault(c => c.Standable(Map));
        }
        GenSpawn.Spawn(pawn, cell.IsValid ? cell : Position, Map);
        UmbralHerald.Mark(pawn, target);
    }

    private void Depart()
    {
        departed = true;
        Map map = Map;
        IntVec3 pos = Position;
        Destroy(DestroyMode.Vanish);
        SkyfallerMaker.SpawnSkyfaller(BOR_UmbralDefOf.BOR_HeraldHarbingerLeaving, pos, map);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref herald, "herald");
        Scribe_References.Look(ref target, "target");
        Scribe_Values.Look(ref ticksUntilRelease, "ticksUntilRelease", ReleaseDelayTicks);
        Scribe_Values.Look(ref departed, "departed");
    }
}

// A refuelable that never runs dry, for the Odyssey Harbinger. The shuttle and caravan code read and burn fuel through
// CompRefuelable directly, so the comp has to stay: it starts full (initialFuelPercent 1), shows nothing, and every
// burn is skipped by the patch below.
public class CompRefuelable_Endless : CompRefuelable
{
    public override string? CompInspectStringExtra() => null;

    // No refuel or out-of-fuel overlays: there is nothing to refuel.
    public override void PostDraw()
    {
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield break;
    }
}

[HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.ConsumeFuel))]
public static class CompRefuelable_ConsumeFuel_Endless
{
    public static bool Prefix(CompRefuelable __instance) => __instance is not CompRefuelable_Endless;
}

// The shuttle always offers "refuel from cargo", greyed out as fully fueled. With no fuel to speak of it only confuses.
[HarmonyPatch(typeof(Building_PassengerShuttle), nameof(Building_PassengerShuttle.GetGizmos))]
public static class Building_PassengerShuttle_GetGizmos_Endless
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Building_PassengerShuttle __instance)
    {
        if (__instance.RefuelableComp is not CompRefuelable_Endless)
        {
            foreach (Gizmo g in __result)
            {
                yield return g;
            }
            yield break;
        }
        string refuelLabel = "CommandRefuelShuttleFromCargo".Translate();
        foreach (Gizmo g in __result)
        {
            if (g is not Command_Action { defaultLabel: var label } || label != refuelLabel)
            {
                yield return g;
            }
        }
    }
}
