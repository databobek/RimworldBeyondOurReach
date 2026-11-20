using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Settings for DamageWorker_Neuralizer, set on the neuralizer DamageDefs.
public class NeuralizerExtension : DefModExtension
{
    public HediffDef? hediff;
    public float severityPerDamage;
}

// Builds the neuralizer hediff from the beam's full damage, scaled down by the electric armor on the hit part.
// CE rounds electrical damage down after armor, so with additionalHediffs a 1-damage beam against any armor dealt 0
// and built nothing. The burn still goes through the normal injury path and keeps CE's rounding.
public class DamageWorker_Neuralizer : DamageWorker_AddInjury
{
    public override DamageResult Apply(DamageInfo dinfo, Thing thing)
    {
        DamageResult result = base.Apply(dinfo, thing);
        // The hit part is recorded even when armor deflects all of the damage.
        if (thing is Pawn pawn && !pawn.Dead && result.LastHitPart is BodyPartRecord part
            && def.GetModExtension<NeuralizerExtension>() is { hediff: HediffDef hediff } settings)
        {
            float severity = dinfo.Amount * settings.severityPerDamage * ArmorFactor(dinfo.Def, pawn, part) / pawn.BodySize;
            if (severity > 0f)
            {
                HealthUtility.AdjustSeverity(pawn, hediff, severity);
            }
        }
        return result;
    }

    // Same sum CE uses for electrical damage: natural armor plus every worn layer covering the part, minus penetration.
    private static float ArmorFactor(DamageDef damage, Pawn pawn, BodyPartRecord part)
    {
        StatDef? stat = damage.armorCategory?.armorRatingStat;
        if (stat == null)
        {
            return 1f;
        }
        float factor = 1f + Mathf.Max(0f, damage.defaultArmorPenetration);
        BodyPartGroupDef? naturalArmor = DefDatabase<BodyPartGroupDef>.GetNamedSilentFail("CoveredByNaturalArmor");
        if (naturalArmor != null && part.IsInGroup(naturalArmor))
        {
            factor -= pawn.GetStatValue(stat);
        }
        if (pawn.apparel != null)
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (apparel.def.apparel.CoversBodyPart(part))
                {
                    factor -= apparel.GetStatValue(stat);
                }
            }
        }
        return Mathf.Clamp01(factor);
    }
}
