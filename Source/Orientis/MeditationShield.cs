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

// Switches the pod's vanilla projectile interceptor, which also stops bombardments, and debilitates dark pawns and
// anomaly creatures in range.
// The switch can be flipped any time, but the shield only stands while an Orientis pawn is meditating in the pod, and it
// switches itself off when the map runs out of meditation power.
public sealed class CompMeditationShield : ThingComp
{
    private const int CheckInterval = 250;
    private const int OccupancyInterval = 60;
    private const string TenebralFaction = "BOR_Tenebrites";

    private static readonly AccessTools.FieldRef<CompProjectileInterceptor, bool> InterceptorShutDown =
        AccessTools.FieldRefAccess<CompProjectileInterceptor, bool>("shutDown");

    // The player's switch, and whether the shield is actually up right now (switch on and a pawn inside).
    private bool enabled;
    private bool active;

    public CompProperties_MeditationShield Props => (CompProperties_MeditationShield)props;

    private CompProjectileInterceptor Interceptor => parent.GetComp<CompProjectileInterceptor>();

    private bool HasPower => MapComponent_MeditationPower.Of(parent.Map).Stored > 0f;

    public static bool IsDark(Pawn pawn) => pawn.RaceProps.Humanlike && (Umbral.IsUmbral(pawn) || Orientis.HasTenebralParts(pawn)
        || pawn.health.hediffSet.HasHediff(BOR_TenebralDefOf.BOR_TenebralPsychosis)
        || IsTenebralFaction(pawn.Faction?.def) || IsTenebralFaction(pawn.kindDef.defaultFactionDef));

    private static bool IsTenebralFaction(FactionDef? faction) => faction?.defName == TenebralFaction;

    private static IEnumerable<CompMeditationShield> ActiveOn(Map map) => map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor)
        .Select(t => t.TryGetComp<CompMeditationShield>()).Where(s => s != null && s.active);

    public static bool Covers(Map map, IntVec3 cell) =>
        ActiveOn(map).Any(s => cell.InHorDistOf(s.parent.Position, s.Interceptor.Props.radius));

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        InterceptorShutDown(Interceptor) = !active;
    }

    private void SetActive(bool value)
    {
        active = value;
        InterceptorShutDown(Interceptor) = !value;
    }

    private void Disable()
    {
        enabled = false;
        SetActive(false);
    }

    // True while an Orientis pawn is meditating in this pod (the job puts it on the pod's cell and hides it).
    private bool Occupied() => parent.Map.thingGrid.ThingsListAtFast(parent.Position)
        .OfType<Pawn>().Any(p => Orientis.IsOrientis(p) && p.CurJobDef == BOR_OrientisDefOf.BOR_OrientisMeditate && p.CurJob.targetA.Thing == parent);

    public override void CompTick()
    {
        base.CompTick();
        if (!enabled)
        {
            return;
        }
        if (parent.IsHashIntervalTick(OccupancyInterval))
        {
            bool shouldRun = HasPower && Occupied();
            if (shouldRun != active)
            {
                SetActive(shouldRun);
            }
        }
        if (!active || !parent.IsHashIntervalTick(CheckInterval))
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
        foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned.Where(p => p.Position.InHorDistOf(parent.Position, radius) && (IsDark(p) || OrientisTargets.IsAnomaly(p))).ToList())
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
        yield return new Command_Toggle
        {
            defaultLabel = "BOR_ShieldToggle".Translate(),
            defaultDesc = "BOR_ShieldDesc".Translate(),
            icon = BorIcons.OrientisMeditate,
            isActive = () => enabled,
            toggleAction = () =>
            {
                if (enabled)
                {
                    Disable();
                }
                else
                {
                    enabled = true;
                }
            }
        };
    }

    public override string CompInspectStringExtra() => (active ? "BOR_ShieldOn" : enabled ? "BOR_ShieldWaiting" : "BOR_ShieldOff").Translate();

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref enabled, "shieldEnabled");
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
