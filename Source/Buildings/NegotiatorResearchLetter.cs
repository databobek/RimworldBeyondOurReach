using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
public static class ResearchManager_FinishProject_NegotiatorLetter_Patch
{
    public static void Postfix(ResearchProjectDef proj)
    {
        if (proj.defName == "BOR_Research_NegotiatorArtillery_II")
        {
            Find.LetterStack.ReceiveLetter("BOR_NegotiatorLetterLabel".Translate(), "BOR_NegotiatorLetterText".Translate(), LetterDefOf.ThreatBig);
        }
    }
}
