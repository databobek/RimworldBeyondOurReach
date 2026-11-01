using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Restricts which research benches an analyzable item can be studied at. Vanilla takes the closest powered bench of any kind.
public class DefModExtension_AnalysisBench : DefModExtension
{
    public List<ThingDef> benches = new();
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class AnalysisBenchPatches
{
    // Vanilla's bench search only receives the pawn, so the item being analyzed is remembered around its two callers.
    private static Thing? analyzing;

    [HarmonyPatch(typeof(CompAnalyzable), nameof(CompAnalyzable.CanInteract))]
    public static class CompAnalyzable_CanInteract_Patch
    {
        public static void Prefix(CompAnalyzable __instance) => analyzing = __instance.parent;

        public static void Finalizer() => analyzing = null;
    }

    [HarmonyPatch(typeof(CompAnalyzable), nameof(CompAnalyzable.OrderForceTarget))]
    public static class CompAnalyzable_OrderForceTarget_Patch
    {
        public static void Prefix(CompAnalyzable __instance) => analyzing = __instance.parent;

        public static void Finalizer() => analyzing = null;
    }

    [HarmonyPatch(typeof(StudyUtility), nameof(StudyUtility.TryFindResearchBench))]
    public static class StudyUtility_TryFindResearchBench_Patch
    {
        public static bool Prefix(Pawn pawn, out Building_ResearchBench? bench, ref bool __result)
        {
            bench = null;
            DefModExtension_AnalysisBench? ext = analyzing?.def.GetModExtension<DefModExtension_AnalysisBench>();
            if (ext == null)
            {
                return true;
            }
            bench = (Building_ResearchBench?)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.ResearchBench), PathEndMode.InteractionCell, TraverseParms.For(pawn),
                validator: t => ext.benches.Contains(t.def) && pawn.CanReserve(t) && (t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true));
            __result = bench != null;
            return false;
        }
    }
}
