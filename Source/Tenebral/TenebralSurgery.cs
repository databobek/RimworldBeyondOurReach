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

    // Taking off a shoulder takes the arm with it, so a part counts as tenebral when anything below it is.
    private static bool HasTenebralOnOrBelow(Pawn pawn, BodyPartRecord part)
    {
        if (HasTenebralOn(pawn, part) || Crystallization.IsCrystallized(pawn, part))
        {
            return true;
        }
        for (int i = 0; i < part.parts.Count; i++)
        {
            if (HasTenebralOnOrBelow(pawn, part.parts[i]))
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

    // Tenebral and crystallized parts never come out by surgery, whether cut off, replaced or lost with a parent part;
    // only implants added beside them still go in. Umbral parts never come out; Umbral pawns take no parts at all;
    // Orientis pawns take no bionics or implants, though a natural transplant is fine.
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

        public static void Postfix(RecipeWorker __instance, Pawn pawn, RecipeDef recipe, ref IEnumerable<BodyPartRecord> __result)
        {
            bool removesPart = __instance is Recipe_RemoveBodyPart;
            bool removesImplant = __instance is Recipe_RemoveImplant;
            bool install = !removesPart && !removesImplant;
            if (install && (Umbral.BlocksBodyParts(pawn)
                || (Orientis.IsOrientis(pawn) && __instance is Recipe_InstallArtificialBodyPart or Recipe_InstallImplant)))
            {
                __result = Enumerable.Empty<BodyPartRecord>();
                return;
            }
            if (removesImplant && recipe.removesHediff != null && recipe.removesHediff.HasModExtension<DefModExtension_TenebralImplant>())
            {
                __result = Enumerable.Empty<BodyPartRecord>();
                return;
            }
            bool replacesPart = removesPart || __instance is Recipe_InstallArtificialBodyPart or Recipe_InstallNaturalBodyPart;
            __result = __result.Where(part =>
                !Crystallization.IsCrystallized(pawn, part)
                && (!replacesPart || !HasTenebralOnOrBelow(pawn, part))
                && (install || !HasUmbralPartOn(pawn, part))).ToList();
        }
    }
}
