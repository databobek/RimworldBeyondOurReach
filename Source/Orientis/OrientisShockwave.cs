using RimWorld;
using Verse;

namespace BeyondOurReach;

public static class OrientisTargets
{
    // Umbrals, Tenebrals and anomaly creatures. The hediffs are checked, not the genes, so it works without Biotech.
    public static bool IsStunTarget(Pawn pawn)
    {
        HediffSet hediffs = pawn.health.hediffSet;
        return hediffs.HasHediff(BOR_UmbralDefOf.BOR_Umbral)
            || hediffs.HasHediff(BOR_UmbralDefOf.BOR_UmbralExoskeleton)
            || hediffs.HasHediff(BOR_OrientisDefOf.BOR_TenebralPsychosis)
            || pawn.RaceProps.IsAnomalyEntity;
    }
}

// Placed on a damage def whose worker stuns targets chosen by OrientisTargets.
public class DefModExtension_TargetStun : DefModExtension
{
    public int stunTicks = 120;
}

// Explosion damage for everyone; a stun only for the targets the weapon was made against.
public class DamageWorker_OrientisBlast : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        DamageResult result = base.Apply(dinfo, victim);
        if (victim is Pawn pawn && !pawn.Dead && OrientisTargets.IsStunTarget(pawn))
        {
            pawn.stances.stunner.StunFor(dinfo.Def.GetModExtension<DefModExtension_TargetStun>().stunTicks, dinfo.Instigator, addBattleLog: false);
        }
        return result;
    }
}
