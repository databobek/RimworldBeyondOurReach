using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Explains the pacts once the dark mirror is researched. The counts come from the BOR_DarkPact quest script.
[HarmonyPatch(typeof(ResearchManager), nameof(ResearchManager.FinishProject))]
public static class ResearchManager_FinishProject_DarkPactLetter_Patch
{
    public static void Postfix(ResearchProjectDef proj)
    {
        if (proj != BOR_HarvestDefOf.BOR_Research_Analyzer_III)
        {
            return;
        }
        QuestNode_Root_DarkPact s = QuestNode_Root_DarkPact.Settings;
        Find.LetterStack.ReceiveLetter("BOR_DarkPact_AboutLabel".Translate(),
            "BOR_DarkPact_AboutText".Translate(QuestNode_Root_DarkPact.DurationLabel, Span(s.people), Span(s.animals), s.burned, s.organs, s.corpses,
                s.ossuaryRadius.ToString("0"), s.guests),
            LetterDefOf.NeutralEvent);
    }

    private static string Span(IntRange range) => range.min == range.max ? range.min.ToString() : $"{range.min}-{range.max}";
}
