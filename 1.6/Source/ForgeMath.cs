using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public static class ForgeMath
{
    private const float BaseTicks = 600f;
    private const float AnalysisMultiplier = 2f;

    public static float TechMultiplier(ThingDef def)
    {
        int tech = (int)def.techLevel;
        return 1f + 0.5f * Mathf.Max(0, tech - 1);
    }

    public static float ValueMultiplier(ThingDef def)
    {
        return 1f + Mathf.Sqrt(Mathf.Max(1f, def.BaseMarketValue)) / 3f;
    }

    // Work ticks at speed 1.0 (Intellectual level 5).
    public static float BaseWorkTicks(ThingDef def)
    {
        return BaseTicks * TechMultiplier(def) * ValueMultiplier(def);
    }

    public static float SpeedFactor(Pawn pawn)
    {
        int level = pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        return 0.5f + level / 10f;
    }

    public static int ProductionTicks(ThingDef def, Pawn pawn)
    {
        return Mathf.Max(30, Mathf.RoundToInt(BaseWorkTicks(def) / SpeedFactor(pawn)));
    }

    public static int AnalysisTicks(ThingDef def, Pawn pawn)
    {
        return Mathf.Max(60, Mathf.RoundToInt(BaseWorkTicks(def) * AnalysisMultiplier / SpeedFactor(pawn)));
    }

    public static float AnalysisSuccessChance(Pawn pawn)
    {
        int level = pawn.skills?.GetSkill(SkillDefOf.Intellectual)?.Level ?? 0;
        return Mathf.Clamp01(level / 25f);
    }

    // Raw, cheap, low-tech, high-stack items give many per run; stack size 1 items always give 1.
    public static int YieldFor(ThingDef def)
    {
        int stack = def.stackLimit;
        if (stack <= 1)
        {
            return 1;
        }
        int tech = (int)def.techLevel;
        float valueFactor = 1f / (1f + def.BaseMarketValue / 30f);
        float techFactor = 1f / (1f + 0.25f * Mathf.Max(0, tech - (int)TechLevel.Industrial));
        float yield = stack * 0.75f * valueFactor * techFactor;
        return Mathf.Clamp(Mathf.RoundToInt(yield), 1, stack);
    }
}
