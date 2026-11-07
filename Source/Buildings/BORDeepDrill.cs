using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// A vanilla deep drill with its own speed and yield. Both are multipliers over a vanilla drill worked by the same pawn.
// Speed is portions per work done; yield is items per portion. The deposit still loses only the vanilla amount per portion.
public class CompProperties_BORDeepDrill : CompProperties_DeepDrill
{
    public float speedMultiplier = 1f;
    public float yieldMultiplier = 1f;

    public CompProperties_BORDeepDrill()
    {
        compClass = typeof(CompBORDeepDrill);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (StatDrawEntry entry in base.SpecialDisplayStats(req))
        {
            yield return entry;
        }
        yield return new StatDrawEntry(StatCategoryDefOf.Basics, "BOR_DrillSpeedMultiplier".Translate(), speedMultiplier.ToStringPercent(),
            "BOR_DrillSpeedMultiplierDesc".Translate(), 4000);
        yield return new StatDrawEntry(StatCategoryDefOf.Basics, "BOR_DrillYieldMultiplier".Translate(), yieldMultiplier.ToStringPercent(),
            "BOR_DrillYieldMultiplierDesc".Translate(), 3999);
    }
}

public class CompBORDeepDrill : CompDeepDrill
{
    // Vanilla keeps the progress private and its only writer is the method the patch below replaces.
    private static readonly AccessTools.FieldRef<CompDeepDrill, float> Progress = AccessTools.FieldRefAccess<CompDeepDrill, float>("portionProgress");
    private static readonly AccessTools.FieldRef<CompDeepDrill, float> YieldPct = AccessTools.FieldRefAccess<CompDeepDrill, float>("portionYieldPct");
    private static readonly AccessTools.FieldRef<CompDeepDrill, int> LastUsedTick = AccessTools.FieldRefAccess<CompDeepDrill, int>("lastUsedTick");
    private static readonly MethodInfo TryProducePortion = AccessTools.Method(typeof(CompDeepDrill), "TryProducePortion");

    private const float WorkPerPortion = 10000f;

    private CompProperties_BORDeepDrill Props => (CompProperties_BORDeepDrill)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        MapComponent_WorkBuildings.Of(parent.Map).drills.Add(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_WorkBuildings.Of(map).drills.Remove(this);
    }

    // Vanilla's DrillWorkDone with the multipliers applied.
    public void BoostedDrillWorkDone(Pawn driller, int delta)
    {
        float work = driller.GetStatValue(StatDefOf.DeepDrillingSpeed) * delta * Props.speedMultiplier;
        ref float progress = ref Progress(this);
        ref float yieldPct = ref YieldPct(this);
        progress += work;
        yieldPct += work * driller.GetStatValue(StatDefOf.MiningYield) * Props.yieldMultiplier / WorkPerPortion;
        LastUsedTick(this) = Find.TickManager.TicksGame;
        if (progress > WorkPerPortion)
        {
            float pct = yieldPct;
            progress = 0f;
            yieldPct = 0f;
            TryProducePortion.Invoke(this, new object[] { pct, driller });
        }
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
[HarmonyPatch(typeof(CompDeepDrill), nameof(CompDeepDrill.DrillWorkDone))]
public static class CompDeepDrill_DrillWorkDone_Patch
{
    public static bool Prefix(CompDeepDrill __instance, Pawn driller, int delta)
    {
        if (__instance is not CompBORDeepDrill drill)
        {
            return true;
        }
        drill.BoostedDrillWorkDone(driller, delta);
        return false;
    }
}

// Vanilla's work giver only looks for the vanilla drill def.
public class WorkGiver_BORDeepDrill : WorkGiver_DeepDrill
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    private static IEnumerable<Building> Drills(Map map) =>
        MapComponent_WorkBuildings.Of(map).drills.Select(d => (Building)d.parent);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => Drills(pawn.Map);

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (Building drill in Drills(pawn.Map))
        {
            if (drill.Faction == pawn.Faction
                && (drill.GetComp<CompPowerTrader>()?.PowerOn ?? true)
                && drill.Map.designationManager.DesignationOn(drill, DesignationDefOf.Uninstall) == null)
            {
                return false;
            }
        }
        return true;
    }
}
