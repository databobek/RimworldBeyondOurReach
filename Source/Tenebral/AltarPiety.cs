using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Dark altar piety: the speed at which a pawn works a dark archotech altar, unsealing and producing alike.
// It starts at the stat's base and gains from an emptier bloodlust, deeper psychosis and every tenebral, umbral or
// crystallized hediff the pawn carries.
public static class AltarPiety
{
    // Bonus at an empty bloodlust; scales down to none at a full one.
    public const float BloodlustWeight = 1f;

    // Bonus per point of psychosis severity (severity 1 = +50%).
    public const float PsychosisWeight = 0.5f;

    // Bonus per tenebral implant or part, per umbral implant or part, and per crystallized part.
    public const float PerPart = 0.05f;

    public static float Bloodlust(Pawn pawn)
    {
        Need_Murder? need = pawn.needs?.TryGetNeed<Need_Murder>();
        return need == null ? 0f : (1f - need.CurLevel) * BloodlustWeight;
    }

    public static float Psychosis(Pawn pawn) => TenebralPsychosis.Severity(pawn) * PsychosisWeight;

    public static int UmbralParts(Pawn pawn) => Count(pawn, h => h.def.HasModExtension<DefModExtension_UmbralPart>());

    public static int CrystallizedParts(Pawn pawn) => Count(pawn, h => h.def == BOR_TenebralDefOf.BOR_Crystallized);

    public static float Bonus(Pawn pawn) =>
        Bloodlust(pawn) + Psychosis(pawn)
        + (TenebralMurder.ImplantCount(pawn) + UmbralParts(pawn) + CrystallizedParts(pawn)) * PerPart;

    private static int Count(Pawn pawn, System.Predicate<Hediff> match)
    {
        int count = 0;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (match(hediffs[i]))
            {
                count++;
            }
        }
        return count;
    }
}

public class StatWorker_AltarPiety : StatWorker
{
    public override bool ShouldShowFor(StatRequest req) =>
        base.ShouldShowFor(req) && req.Thing is Pawn pawn && pawn.RaceProps.Humanlike && TenebralPsychosis.IsPsychotic(pawn);

    public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
    {
        float value = base.GetValueUnfinalized(req, applyPostProcess);
        return req.Thing is Pawn pawn ? value + AltarPiety.Bonus(pawn) : value;
    }

    public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
    {
        StringBuilder sb = new(base.GetExplanationUnfinalized(req, numberSense));
        if (req.Thing is Pawn pawn)
        {
            sb.AppendLine();
            Line(sb, "BOR_AltarPietyBloodlust", AltarPiety.Bloodlust(pawn));
            Line(sb, "BOR_AltarPietyPsychosis", AltarPiety.Psychosis(pawn));
            Line(sb, "BOR_AltarPietyTenebral", TenebralMurder.ImplantCount(pawn) * AltarPiety.PerPart);
            Line(sb, "BOR_AltarPietyUmbral", AltarPiety.UmbralParts(pawn) * AltarPiety.PerPart);
            Line(sb, "BOR_AltarPietyCrystallized", AltarPiety.CrystallizedParts(pawn) * AltarPiety.PerPart);
        }
        return sb.ToString();
    }

    private static void Line(StringBuilder sb, string key, float value)
    {
        if (value > 0f)
        {
            sb.AppendLine($"    {key.Translate()}: {value.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset)}");
        }
    }
}
