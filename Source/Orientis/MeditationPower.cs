using System.Collections.Generic;
using System.Linq;
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

    // The nearest cache within its load radius that still has room.
    public CompMeditationCache? CacheToLoad(IntVec3 from) => caches
        .Where(c => !c.Full && c.parent.Position.InHorDistOf(from, c.Props.loadRadius))
        .OrderBy(c => c.parent.Position.DistanceToSquared(from))
        .FirstOrDefault();
}

public class CompProperties_MeditationCache : CompProperties
{
    public float capacity = 1000f;

    // How close a meditating pawn has to be to load this cache.
    public float loadRadius = 12f;

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

    public void Add(float amount) => stored = Mathf.Min(Props.capacity, stored + amount);

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
        "BOR_MeditationCacheInspect".Translate(stored.ToString("F0"), Props.capacity.ToString("F0"));

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref stored, "meditationStored");
    }
}

// The meditation symbol and a bar showing how full this cache is.
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
        Widgets.FillableBar(bar, cache.Stored / cache.Props.capacity);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(bar, cache.Stored.ToString("F0") + " / " + cache.Props.capacity.ToString("F0"));
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
        return new GizmoResult(GizmoState.Clear);
    }
}

public class CompProperties_MeditationSpot : CompProperties
{
    // Power per day at a meditation strength of 100%.
    public float powerPerDay = 200f;

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
        if (MapComponent_MeditationPower.Of(t.Map).CacheToLoad(t.Position) == null)
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

    private Thing Spot => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Spot, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, Spot.def.hasInteractionCell ? PathEndMode.InteractionCell : PathEndMode.OnCell);

        Toil meditate = ToilMaker.MakeToil("OrientisMeditate");
        meditate.tickAction = () =>
        {
            pawn.rotationTracker.FaceTarget(Spot);
            if (!pawn.IsHashIntervalTick(LoadInterval))
            {
                return;
            }
            CompMeditationCache? cache = MapComponent_MeditationPower.Of(pawn.Map).CacheToLoad(Spot.Position);
            if (cache == null)
            {
                EndJobWith(JobCondition.Succeeded);
                return;
            }
            float perDay = Spot.TryGetComp<CompMeditationSpot>().Props.powerPerDay;
            cache.Add(perDay * pawn.GetStatValue(BOR_OrientisDefOf.BOR_MeditationStrength) * LoadInterval / GenDate.TicksPerDay);
        };
        meditate.defaultCompleteMode = ToilCompleteMode.Delay;
        meditate.defaultDuration = SessionTicks;
        meditate.handlingFacing = true;
        yield return meditate;
    }
}
