using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class TenebralSurgeryPatches
{
    private static bool HasTenebralOn(Pawn pawn, BodyPartRecord part)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].Part == part && hediffs[i].def.HasModExtension<DefModExtension_TenebralImplant>())
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasUmbralPartOn(Pawn pawn, BodyPartRecord part)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].Part == part && hediffs[i].def.HasModExtension<DefModExtension_UmbralPart>())
            {
                return true;
            }
        }
        return false;
    }

    // Crystallized parts can't be removed or replaced; tenebral bionics come out only past the holytech threshold;
    // Umbral parts never come out; Umbral pawns take no parts at all.
    [HarmonyPatch]
    public static class Recipe_GetPartsToApplyOn_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Recipe_RemoveBodyPart), nameof(RecipeWorker.GetPartsToApplyOn));
            yield return AccessTools.Method(typeof(Recipe_RemoveBodyPart_Cut), nameof(RecipeWorker.GetPartsToApplyOn));
            yield return AccessTools.Method(typeof(Recipe_RemoveBodyPart_CutMany), nameof(RecipeWorker.GetPartsToApplyOn));
            yield return AccessTools.Method(typeof(Recipe_RemoveImplant), nameof(RecipeWorker.GetPartsToApplyOn));
            yield return AccessTools.Method(typeof(Recipe_InstallArtificialBodyPart), nameof(RecipeWorker.GetPartsToApplyOn));
            yield return AccessTools.Method(typeof(Recipe_InstallNaturalBodyPart), nameof(RecipeWorker.GetPartsToApplyOn));
            yield return AccessTools.Method(typeof(Recipe_InstallImplant), nameof(RecipeWorker.GetPartsToApplyOn));
        }

        public static void Postfix(RecipeWorker __instance, Pawn pawn, ref IEnumerable<BodyPartRecord> __result)
        {
            bool install = __instance is not (Recipe_RemoveBodyPart or Recipe_RemoveImplant);
            if (install && Umbral.BlocksBodyParts(pawn))
            {
                __result = Enumerable.Empty<BodyPartRecord>();
                return;
            }
            bool holytech = TenebralPsychosis.CanTakeHolytechExit(pawn);
            __result = __result.Where(part =>
                !Crystallization.IsCrystallized(pawn, part)
                && (install || ((holytech || !HasTenebralOn(pawn, part)) && !HasUmbralPartOn(pawn, part)))).ToList();
        }
    }

    // A tenebral bionic removed past the holytech threshold opens the Holytech path.
    [HarmonyPatch]
    public static class Recipe_Remove_ApplyOnPawn_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Recipe_RemoveBodyPart), nameof(RecipeWorker.ApplyOnPawn));
            yield return AccessTools.Method(typeof(Recipe_RemoveImplant), nameof(RecipeWorker.ApplyOnPawn));
        }

        public static void Prefix(Pawn pawn, BodyPartRecord part, out bool __state)
        {
            __state = part != null && HasTenebralOn(pawn, part);
        }

        public static void Postfix(Pawn pawn, BodyPartRecord part, bool __state)
        {
            if (!__state || HasTenebralOn(pawn, part) || pawn.health.hediffSet.HasHediff(BOR_TenebralDefOf.BOR_HolytechCandidate))
            {
                return;
            }
            pawn.health.AddHediff(BOR_TenebralDefOf.BOR_HolytechCandidate);
            if (pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message("BOR_HolytechCandidate".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
