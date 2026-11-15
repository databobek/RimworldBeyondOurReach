using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

[StaticConstructorOnStartup]
public static class OrientisTextures
{
    public static readonly Texture2D MeditationPower = ContentFinder<Texture2D>.Get("BOR/UI/Orientis/MeditationPower");
}

// Meditation power is one pool per map: no grid, every consumer can draw from every cache.
public class MapComponent_MeditationPower : MapComponent
{
    private readonly List<CompMeditationCache> caches = new();

    public MapComponent_MeditationPower(Map map) : base(map)
    {
    }

    public static MapComponent_MeditationPower Of(Map map) => map.GetComponent<MapComponent_MeditationPower>();

    public float Stored => caches.Sum(c => c.Stored);

    public float Capacity => caches.Sum(c => c.Props.capacity);

    public void Register(CompMeditationCache cache) => caches.Add(cache);

    public void Unregister(CompMeditationCache cache) => caches.Remove(cache);

    public bool TryDraw(float amount)
    {
        if (Stored < amount)
        {
            return false;
        }
        foreach (CompMeditationCache cache in caches)
        {
            amount -= cache.Draw(amount);
            if (amount <= 0f)
            {
                break;
            }
        }
        return true;
    }

    public bool HasRoom => caches.Any(c => !c.Full);

    // Shared evenly between the caches that have room, wherever they are on the map.
    public void Add(float amount)
    {
        while (amount > 0.001f)
        {
            List<CompMeditationCache> open = caches.Where(c => !c.Full).ToList();
            if (open.Count == 0)
            {
                return;
            }
            float each = amount / open.Count;
            foreach (CompMeditationCache cache in open)
            {
                amount -= cache.Add(each);
            }
        }
    }
}

public class CompProperties_MeditationCache : CompProperties
{
    public float capacity = 1000f;

    public CompProperties_MeditationCache()
    {
        compClass = typeof(CompMeditationCache);
    }
}

public class CompMeditationCache : ThingComp
{
    private float stored;

    public CompProperties_MeditationCache Props => (CompProperties_MeditationCache)props;

    public float Stored => stored;

    public bool Full => stored >= Props.capacity;

    // Returns what it could actually take.
    public float Add(float amount)
    {
        float added = Mathf.Min(Props.capacity - stored, amount);
        stored += added;
        return added;
    }

    // Returns what it could actually give.
    public float Draw(float amount)
    {
        float drawn = Mathf.Min(stored, amount);
        stored -= drawn;
        return drawn;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        MapComponent_MeditationPower.Of(parent.Map).Register(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_MeditationPower.Of(map).Unregister(this);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        yield return new Gizmo_MeditationPower(this);
        if (DebugSettings.godMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Fill",
                action = () => stored = Props.capacity
            };
        }
    }

    public override string CompInspectStringExtra() =>
        "BOR_MeditationCacheInspect".Translate(MapComponent_MeditationPower.Of(parent.Map).Stored.ToString("F0"), MapComponent_MeditationPower.Of(parent.Map).Capacity.ToString("F0"));

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref stored, "meditationStored");
    }
}

// The meditation symbol and a bar showing how full the whole map's pool is.
public class Gizmo_MeditationPower : Gizmo
{
    private const float Width = 160f;
    private const float IconSize = 32f;

    private readonly CompMeditationCache cache;

    public Gizmo_MeditationPower(CompMeditationCache cache)
    {
        this.cache = cache;
        Order = -100f;
    }

    public override float GetWidth(float maxWidth) => Width;

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        Rect rect = new(topLeft.x, topLeft.y, Width, Height);
        Widgets.DrawWindowBackground(rect);
        Rect inner = rect.ContractedBy(6f);
        GUI.DrawTexture(new Rect(inner.x, inner.y, IconSize, IconSize), OrientisTextures.MeditationPower);
        Text.Font = GameFont.Tiny;
        Widgets.Label(new Rect(inner.x + IconSize + 4f, inner.y, inner.width - IconSize - 4f, IconSize), "BOR_MeditationPower".Translate());
        Rect bar = new(inner.x, inner.yMax - 22f, inner.width, 22f);
        MapComponent_MeditationPower power = MapComponent_MeditationPower.Of(cache.parent.Map);
        Widgets.FillableBar(bar, power.Stored / power.Capacity);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(bar, power.Stored.ToString("F0") + " / " + power.Capacity.ToString("F0"));
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        return new GizmoResult(GizmoState.Clear);
    }
}

public class CompProperties_MeditationSpot : CompProperties
{
    // Power per day at a meditation strength of 100%.
    public float powerPerDay = 200f;

    // The pawn sits in the middle of the building, facing sideways across it, instead of beside it.
    public bool sitInMiddle;

    // The pawn goes inside and is not drawn, like in a cryptosleep casket.
    public bool hidePawn;

    public CompProperties_MeditationSpot()
    {
        compClass = typeof(CompMeditationSpot);
    }
}

// A pod, bench or mat. They differ only in how much power they make.
public sealed class CompMeditationSpot : ThingComp
{
    public CompProperties_MeditationSpot Props => (CompProperties_MeditationSpot)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        MapComponent_WorkBuildings.Of(parent.Map).meditationSpots.Add(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_WorkBuildings.Of(map).meditationSpots.Remove(this);
    }
}

public class WorkGiver_OrientisMeditate : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool Prioritized => true;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !Orientis.IsOrientis(pawn);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) =>
        MapComponent_WorkBuildings.Of(pawn.Map).meditationSpots.Where(s => s.parent.Faction == Faction.OfPlayer).Select(s => s.parent);

    // The strongest free spot first, so pawns only settle for a mat when the pods are taken.
    public override float GetPriority(Pawn pawn, TargetInfo t) => t.Thing.TryGetComp<CompMeditationSpot>().Props.powerPerDay;

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        if (!MapComponent_MeditationPower.Of(t.Map).HasRoom)
        {
            JobFailReason.Is("BOR_MeditationNoCache".Translate());
            return null;
        }
        return JobMaker.MakeJob(BOR_OrientisDefOf.BOR_OrientisMeditate, t);
    }
}

// Meditates at the spot for a while, loading the nearest cache, and goes back to the work scan.
public class JobDriver_OrientisMeditate : JobDriver
{
    private const int SessionTicks = 5000;
    private const int LoadInterval = 60;

    private bool moved;

    // Saved, as a job loaded mid-session does not run the toil's initAction again.
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref moved, "moved");
    }

    private Thing Spot => job.GetTarget(TargetIndex.A).Thing;

    private CompProperties_MeditationSpot SpotProps => Spot.TryGetComp<CompMeditationSpot>().Props;

    // True while the pawn is inside a pod, which stops it being drawn.
    public bool Hidden => moved && SpotProps.hidePawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Spot, job, 1, -1, null, errorOnFailed);

    // Across the building: sideways to a vertical one, up or down for a horizontal one. Which way is the pawn's own.
    private Rot4 SeatFacing()
    {
        bool second = pawn.thingIDNumber % 2 == 0;
        return Spot.Rotation.IsHorizontal ? (second ? Rot4.North : Rot4.South) : (second ? Rot4.East : Rot4.West);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, Spot.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.OnCell);

        Toil meditate = ToilMaker.MakeToil("OrientisMeditate");
        meditate.initAction = () =>
        {
            CompProperties_MeditationSpot spot = SpotProps;
            if (spot.sitInMiddle || spot.hidePawn)
            {
                pawn.Position = Spot.Position;
                pawn.Notify_Teleported(false, true);
                moved = true;
            }
        };
        meditate.AddFinishAction(() =>
        {
            if (moved && pawn.Spawned && Spot.Spawned)
            {
                pawn.Position = Spot.InteractionCell;
                pawn.Notify_Teleported(false, true);
            }
            moved = false;
        });
        meditate.tickAction = () =>
        {
            if (moved && SpotProps.sitInMiddle)
            {
                pawn.Rotation = SeatFacing();
            }
            else
            {
                pawn.rotationTracker.FaceTarget(Spot);
            }
            if (!pawn.IsHashIntervalTick(LoadInterval))
            {
                return;
            }
            MapComponent_MeditationPower power = MapComponent_MeditationPower.Of(pawn.Map);
            if (!power.HasRoom)
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }
            float perDay = Spot.TryGetComp<CompMeditationSpot>().Props.powerPerDay;
            power.Add(perDay * pawn.GetStatValue(BOR_OrientisDefOf.BOR_MeditationStrength) * LoadInterval / GenDate.TicksPerDay);
        };
        meditate.defaultCompleteMode = ToilCompleteMode.Delay;
        meditate.defaultDuration = SessionTicks;
        meditate.handlingFacing = true;
        yield return meditate;
    }
}

// A pawn inside a meditation pod is not drawn.
[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
public static class PawnRenderer_RenderPawnAt_Patch
{
    private static readonly AccessTools.FieldRef<PawnRenderer, Pawn> PawnOf = AccessTools.FieldRefAccess<PawnRenderer, Pawn>("pawn");

    public static bool Prefix(PawnRenderer __instance) =>
        PawnOf(__instance).jobs?.curDriver is not JobDriver_OrientisMeditate { Hidden: true };
}
