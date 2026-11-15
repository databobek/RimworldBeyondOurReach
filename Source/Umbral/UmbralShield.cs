using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

public class CompProperties_UmbralShield : CompProperties
{
    public CompProperties_UmbralShield()
    {
        compClass = typeof(CompUmbralShield);
    }
}

// Sits next to a vanilla projectile interceptor, which stops the fire. Everything else the dome does looks up Covers.
public sealed class CompUmbralShield : ThingComp
{
    private bool domeHidden;

    public bool DomeHidden => domeHidden;

    public bool Powered => parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false;

    // Only a dome that is up covers anything: vanilla's interceptor flag, which the patch below also drops without power.
    // The flare patch keeps its own list of domes for each tick.
    public static bool Covers(Map map, IntVec3 cell)
    {
        List<Thing> things = map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i].TryGetComp<CompUmbralShield>() == null)
            {
                continue;
            }
            CompProjectileInterceptor interceptor = things[i].TryGetComp<CompProjectileInterceptor>();
            if (interceptor.Active && cell.InHorDistOf(things[i].Position, interceptor.Props.radius))
            {
                return true;
            }
        }
        return false;
    }

    public override string? CompInspectStringExtra()
    {
        return parent.Spawned ? RTSolarFlareShieldCompat.InterferenceMessage(parent.Map) : null;
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
            defaultLabel = "BOR_UmbralShieldShowDome".Translate(),
            defaultDesc = "BOR_UmbralShieldShowDomeDesc".Translate(),
            icon = parent.def.uiIcon,
            isActive = () => !domeHidden,
            toggleAction = () => domeHidden = !domeHidden
        };
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref domeHidden, "domeHidden");
    }
}

public class ThoughtWorker_UnderUmbralShield : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p) =>
        p.Spawned && Orientis.IsOrientis(p) && CompUmbralShield.Covers(p.Map, p.Position);
}

// Vanilla interceptors do not look at power, but this dome falls without it.
[HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.Active), MethodType.Getter)]
public static class CompProjectileInterceptor_Active_Patch
{
    public static void Postfix(CompProjectileInterceptor __instance, ref bool __result)
    {
        if (__result && __instance.parent.TryGetComp<CompUmbralShield>() is { Powered: false })
        {
            __result = false;
        }
    }
}

// Only the drawing stops; a hidden dome still intercepts.
[HarmonyPatch(typeof(CompProjectileInterceptor), nameof(CompProjectileInterceptor.PostDraw))]
public static class CompProjectileInterceptor_PostDraw_Patch
{
    public static bool Prefix(CompProjectileInterceptor __instance) =>
        !(__instance.parent.TryGetComp<CompUmbralShield>()?.DomeHidden ?? false);
}

// Every drop pod spot search goes through this, so pods land outside the dome. Shuttles use a different check.
[HarmonyPatch(typeof(DropCellFinder), nameof(DropCellFinder.CanPhysicallyDropInto))]
public static class DropCellFinder_CanPhysicallyDropInto_Patch
{
    public static void Postfix(IntVec3 c, Map map, ref bool __result)
    {
        if (__result && CompUmbralShield.Covers(map, c))
        {
            __result = false;
        }
    }
}
