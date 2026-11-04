using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class HediffCompProperties_OrientisArmorBreak : HediffCompProperties
{
    public float disableChance = 0.3f;
    public float lockSeconds = 30f;

    public HediffCompProperties_OrientisArmorBreak()
    {
        compClass = typeof(HediffComp_OrientisArmorBreak);
    }
}

// On landing, rolls to knock the Umbral exoskeleton out of combat mode.
public class HediffComp_OrientisArmorBreak : HediffComp
{
    public HediffCompProperties_OrientisArmorBreak Props => (HediffCompProperties_OrientisArmorBreak)props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        if (!Rand.Chance(Props.disableChance))
        {
            return;
        }
        Hediff? exo = Pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskeleton);
        exo?.TryGetComp<HediffComp_UmbralModeApparel>()?.TryDisableCombat(Mathf.RoundToInt(Props.lockSeconds * GenTicks.TicksPerRealSecond));
    }
}

// Adds the armor-break hediff to Umbrals standing in combat mode, after the shot has done its damage.
public class DamageWorker_OrientisLaser : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        DamageResult result = base.Apply(dinfo, victim);
        if (victim is Pawn pawn && !pawn.Dead)
        {
            Hediff? exo = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskeleton);
            if (exo?.TryGetComp<HediffComp_UmbralModeApparel>()?.Mode == ExoskeletonMode.Combat)
            {
                pawn.health.AddHediff(BOR_OrientisDefOf.BOR_OrientisArmorBreak, dinfo: dinfo);
            }
        }
        return result;
    }
}
