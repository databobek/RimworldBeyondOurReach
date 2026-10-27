using RimWorld;
using Verse;

namespace BeyondOurReach;

[DefOf]
public static class BOR_HarvestDefOf
{
    public static JobDef BOR_HarvestComponents = null!;

    public static ThingDef BOR_ComponentIII = null!;
    public static ThingDef BOR_Picoclusters = null!;

    public static ThoughtDef BOR_MutilatedBody = null!;
    public static ThoughtDef BOR_MutilatedBodySocial = null!;

    public static ResearchProjectDef BOR_Research_ComponentHarvest = null!;

    static BOR_HarvestDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_HarvestDefOf));
    }
}
