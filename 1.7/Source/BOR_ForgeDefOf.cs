using RimWorld;
using Verse;

namespace BeyondOurReach;

[DefOf]
public static class BOR_ForgeDefOf
{
    public static JobDef BOR_ForgeAnalyze = null!;
    public static JobDef BOR_ForgeProduce = null!;

    public static StatDef BOR_ForgeAnalysis = null!;
    public static StatDef BOR_QuantumForging = null!;

    static BOR_ForgeDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_ForgeDefOf));
    }
}
