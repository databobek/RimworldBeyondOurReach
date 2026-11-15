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
            || IsAnomaly(pawn);
    }

    // Entity races (fleshbeasts, entity flesh and mechanical entities) and every mutant: shamblers, awoken corpses and
    // ghouls, the colony's own included. Mutants keep a human race, so the race check alone misses them.
    public static bool IsAnomaly(Pawn pawn) => pawn.RaceProps.IsAnomalyEntity || pawn.IsMutant;
}

// Placed on a damage def whose worker stuns targets chosen by OrientisTargets.
public class DefModExtension_TargetStun : DefModExtension
{
    public int stunTicks = 120;

    // Stun for every other pawn. Zero leaves them alone.
    public int otherStunTicks;
}

// Explosion damage for everyone; a stun only for the targets the weapon was made against.
public class DamageWorker_OrientisBlast : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing victim)
    {
        DamageResult result = base.Apply(dinfo, victim);
        if (victim is Pawn pawn && !pawn.Dead)
        {
            DefModExtension_TargetStun stun = dinfo.Def.GetModExtension<DefModExtension_TargetStun>();
            int ticks = OrientisTargets.IsStunTarget(pawn) ? stun.stunTicks : stun.otherStunTicks;
            if (ticks > 0)
            {
                pawn.stances.stunner.StunFor(ticks, dinfo.Instigator, addBattleLog: false);
            }
        }
        return result;
    }
}

