using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Placed on a pawn kind whose pawns are generated as finished Umbrals.
public class DefModExtension_UmbralFullyGrown : DefModExtension
{
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
public static class PawnGenerator_GeneratePawn_UmbralFullyGrown_Patch
{
    // Every transformation step is one call, so this is the most the three phases can take.
    private const int MaxSteps = 64;

    public static void Postfix(Pawn __result)
    {
        if (__result?.kindDef?.HasModExtension<DefModExtension_UmbralFullyGrown>() != true)
        {
            return;
        }
        Umbral.MakeUmbral(__result);
        for (int i = 0; i < MaxSteps && Umbral.CurrentPhase(__result) != Umbral.Phase.Done; i++)
        {
            Umbral.CompleteTransformation(__result);
        }
        // The abilities are random in normal play; this kind always has all four.
        __result.abilities ??= new Pawn_AbilityTracker(__result);
        foreach (AbilityDef ability in new[] { BOR_DarkAbilityDefOf.BOR_DarkWound, BOR_DarkAbilityDefOf.BOR_DarkKill, BOR_DarkAbilityDefOf.BOR_DarkThrall, BOR_DarkAbilityDefOf.BOR_DarkBolt })
        {
            __result.abilities.GainAbility(ability);
        }
        // They arrive in no form with a full charge; the suit AI puts the combat armor on when enemies come near.
    }
}
