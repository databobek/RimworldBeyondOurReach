using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class CompProperties_MeditationShield : CompProperties
{
    public float powerPerDay = 400f;

    public CompProperties_MeditationShield()
    {
        compClass = typeof(CompMeditationShield);
    }
}

// Switches the pod's vanilla projectile interceptor, which also stops bombardments, and debilitates dark pawns in range.
// It stays off until an Orientis pawn walks over and raises it, and falls when the map runs out of meditation power.
public class CompMeditationShield : ThingComp
{
    private const int CheckInterval = 250;
    private const string TenebralFaction = "BOR_Tenebrites";

    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, bool> InterceptorShutDown =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, bool>("shutDown");

    private bool enabled;

    public CompProperties_MeditationShield Props => (CompProperties_MeditationShield)props;

    public bool Enabled => enabled;

    private CompProjectileInterceptor Interceptor => parent.GetComp<CompProjectileInterceptor>();

    private bool HasPower => MapComponent_MeditationPower.Of(parent.Map).Stored > 0f;

    public static bool IsDark(Pawn pawn) => pawn.RaceProps.Humanlike && (Umbral.IsUmbral(pawn) || Orientis.HasTenebralParts(pawn)
        || pawn.health.hediffSet.HasHediff(BOR_TenebralDefOf.BOR_TenebralPsychosis)
        || IsTenebralFaction(pawn.Faction?.def) || IsTenebralFaction(pawn.kindDef.defaultFactionDef));

    private static bool IsTenebralFaction(FactionDef? faction) => faction?.defName == TenebralFaction;

    private static IEnumerable<CompMeditationShield> ActiveOn(Map map) => map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
        .Select(t => t.TryGetComp<CompMeditationShield>()).Where(s => s != null && s.enabled);

    public static bool Covers(Map map, IntVec3 cell) =>
        ActiveOn(map).Any(s => cell.InHorDistOf(s.parent.Position, s.Interceptor.Props.radius));

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        InterceptorShutDown(Interceptor) = !enabled;
    }

    public void Enable()
    {
        if (enabled || !parent.Spawned || !HasPower)
        {
            return;
        }
        enabled = true;
        InterceptorShutDown(Interceptor) = false;
    }

    private void Disable()
    {
        enabled = false;
        InterceptorShutDown(Interceptor) = true;
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!enabled || !parent.IsHashIntervalTick(CheckInterval))
        {
            return;
        }
        if (!MapComponent_MeditationPower.Of(parent.Map).TryDraw(Props.powerPerDay * CheckInterval / GenDate.TicksPerDay))
        {
            Disable();
            Messages.Message("BOR_ShieldNoPower".Translate(), parent, MessageTypeDefOf.NegativeEvent);
            return;
        }
        float radius = Interceptor.Props.radius;
        foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned.Where(p => p.Position.InHorDistOf(parent.Position, radius) && IsDark(p)).ToList())
        {
            if (!pawn.health.hediffSet.HasHediff(BOR_OrientisDefOf.BOR_ShieldExposure))
            {
                pawn.health.AddHediff(BOR_OrientisDefOf.BOR_ShieldExposure);
            }
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra())
        {
            yield return gizmo;
        }
        if (parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        if (enabled)
        {
            yield return new Command_Action
            {
                defaultLabel = "BOR_ShieldDisable".Translate(),
                defaultDesc = "BOR_ShieldDesc".Translate(),
                icon = OrientisTextures.MeditationPower,
                action = Disable
            };
            yield break;
        }
        List<Pawn> pawns = parent.Map.mapPawns.FreeColonistsSpawned
            .Where(p => Orientis.IsOrientis(p) && !p.Downed && p.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly)).ToList();
        Command_Action enable = new()
        {
            defaultLabel = "BOR_ShieldEnable".Translate(),
            defaultDesc = "BOR_ShieldDesc".Translate(),
            icon = OrientisTextures.MeditationPower,
            action = () => Find.WindowStack.Add(new FloatMenu(pawns.Select(p => new FloatMenuOption(p.LabelShortCap, () =>
                p.jobs.TryTakeOrderedJob(JobMaker.MakeJob(BOR_OrientisDefOf.BOR_OrientisEnableShield, parent), JobTag.Misc))).ToList()))
        };
        if (!HasPower)
        {
            enable.Disable("BOR_ShieldNoCharge".Translate());
        }
        else if (pawns.Count == 0)
        {
            enable.Disable("BOR_ShieldNoPawn".Translate());
        }
        yield return enable;
    }

    public override string CompInspectStringExtra() => (enabled ? "BOR_ShieldOn" : "BOR_ShieldOff").Translate();

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref enabled, "shieldEnabled");
    }
}

// Walks to the pod and raises its shield. No reservation, so a pawn meditating in the pod does not block it.
public class JobDriver_OrientisEnableShield : JobDriver
{
    private const int RaiseTicks = 120;

    private Thing Pod => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        yield return Toils_General.Wait(RaiseTicks, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);

        Toil raise = ToilMaker.MakeToil("OrientisEnableShield");
        raise.initAction = () => Pod.TryGetComp<CompMeditationShield>().Enable();
        raise.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return raise;
    }
}

public class HediffCompProperties_ShieldExposure : HediffCompProperties
{
    public float severityPerHour = 0.05f;

    public HediffCompProperties_ShieldExposure()
    {
        compClass = typeof(HediffComp_ShieldExposure);
    }
}

// Grows while the pawn stands under a raised shield, and ends as soon as it does not.
public class HediffComp_ShieldExposure : HediffComp
{
    private const int CheckInterval = 250;
    private const int GlowInterval = 30;

    private static readonly UnityEngine.Color GlowColor = new(1f, 0.8f, 0.25f, 1f);

    public HediffCompProperties_ShieldExposure Props => (HediffCompProperties_ShieldExposure)props;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (Pawn.Spawned && Pawn.IsHashIntervalTick(GlowInterval, delta))
        {
            FleckCreationData glow = FleckMaker.GetDataStatic(Pawn.DrawPos, Pawn.Map, FleckDefOf.LightningGlow, 3f);
            glow.instanceColor = GlowColor;
            Pawn.Map.flecks.CreateFleck(glow);
        }
        if (!Pawn.IsHashIntervalTick(CheckInterval, delta))
        {
            return;
        }
        if (Pawn.Spawned && CompMeditationShield.Covers(Pawn.Map, Pawn.Position))
        {
            severityAdjustment += Props.severityPerHour * CheckInterval / GenDate.TicksPerHour;
        }
        else
        {
            severityAdjustment -= parent.Severity;
        }
    }
}

// Vanilla stops bombardments but not the power beam, which is not made of projectiles and moves around.
[HarmonyPatch(typeof(PowerBeam), "Tick")]
public static class PowerBeam_Tick_Patch
{
    public static bool Prefix(PowerBeam __instance)
    {
        if (__instance.instigator == null || !__instance.instigator.HostileTo(Faction.OfPlayer) || !CompMeditationShield.Covers(__instance.Map, __instance.Position))
        {
            return true;
        }
        __instance.Destroy();
        return false;
    }
}
