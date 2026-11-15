using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// The card shown next to the mouse while it is over a pawn in the Tenebral Pod window: what the pod's actions care
// about at a glance (health for healing and stasis, crystallization for transformation, tenebral implants for
// operations), plus whether the pawn can be picked and how they would get to the pod. Drawn through BorSkin in the
// Tenebral scheme, as a floating window of its own so it is not clipped by the pod window.
public static class TenebralPodPawnCard
{
    private const int WindowId = 0x7E4E8A1;
    private const float Width = 320f;
    private const float Pad = 10f;
    private const float Portrait = 64f;
    private const float BarHeight = 20f;
    private const float Gap = 6f;
    private const int MaxConditions = 6;

    private static readonly Vector3 PortraitOffset = new(0f, 0f, 0.3f);
    private static HashSet<HediffDef>? implantDefs;

    // Hediffs the pod's operations install.
    private static HashSet<HediffDef> ImplantDefs => implantDefs ??=
        new HashSet<HediffDef>(Building_TenebralPod.Operations.Where(r => r.addsHediff != null).Select(r => r.addsHediff));

    // role: Colonist / Slave / Prisoner as the window lists them; blocked: why the pawn can't be picked for the current
    // action (null if they can); arrival: walks or carried (empty for the pawn already inside).
    public static void Show(Pawn pawn, string role, string? blocked, string arrival)
    {
        BorSkin skin = BorSkin.Current;
        float height = Draw(new Rect(0f, 0f, Width, 9999f), pawn, role, blocked, arrival, measure: true);
        Vector2 mouse = UI.MousePositionOnUIInverted;
        float x = mouse.x + 24f + Width <= UI.screenWidth ? mouse.x + 24f : mouse.x - 24f - Width;
        float y = Mathf.Clamp(mouse.y - 20f, 0f, Mathf.Max(0f, UI.screenHeight - height));
        Rect rect = new(x, y, Width, height);
        Find.WindowStack.ImmediateWindow(WindowId, rect, WindowLayer.Super, () =>
        {
            BorSkin s = BorSkin.Current;
            s.PushScheme(BOR_UISchemeDefOf.BOR_UIScheme_Tenebral);
            try
            {
                Rect local = rect.AtZero();
                s.Popup(local);
                Draw(local, pawn, role, blocked, arrival, measure: false);
            }
            catch (Exception e)
            {
                BorSkin.Blame(e);
                throw;
            }
            finally
            {
                s.PopScheme();
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
            }
        }, doBackground: !skin.IsThemed);
    }

    // Lays the card out in rect; draws it unless measuring. Returns the height it takes.
    private static float Draw(Rect rect, Pawn pawn, string role, string? blocked, string arrival, bool measure)
    {
        BorSkin skin = BorSkin.Current;
        float x = rect.x + Pad;
        float width = rect.width - Pad * 2f;
        float y = rect.y + Pad;

        // Portrait, name, and who they are.
        Rect portrait = new(x, y, Portrait, Portrait);
        float textX = portrait.xMax + 10f;
        float textWidth = rect.xMax - Pad - textX;
        if (!measure)
        {
            skin.Section(portrait);
            GUI.DrawTexture(portrait.ContractedBy(2f), PortraitsCache.Get(pawn, new Vector2(Portrait, Portrait), Rot4.South, PortraitOffset, 1.28f));
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(textX, y, textWidth, 30f), pawn.LabelShortCap.Truncate(textWidth));
            Text.Font = GameFont.Small;
            GUI.color = skin.TextDim;
            Widgets.Label(new Rect(textX, y + 30f, textWidth, 34f), Who(pawn, role));
            GUI.color = Color.white;
        }
        y = portrait.yMax + Gap + 2f;

        // Health: what healing and stasis are for.
        float health = pawn.health.summaryHealth.SummaryHealthPercent;
        string healthLabel = "BOR_PodCard_Health".Translate(health.ToStringPercent());
        if (pawn.Downed)
        {
            healthLabel += ", " + "BOR_PodCard_Downed".Translate();
        }
        if (pawn.health.hediffSet.BleedRateTotal > 0.01f)
        {
            healthLabel += ", " + "BOR_PodCard_Bleeding".Translate();
        }
        if (!measure)
        {
            skin.Bar(new Rect(x, y, width, BarHeight), health, healthLabel, health < 0.5f ? BorTone.Warn : BorTone.Info);
        }
        y += BarHeight + Gap;

        // Crystallization: transformation needs at least the pod's threshold.
        float crystal = Crystallization.Fraction(pawn);
        float needed = TenebralPsychosis.BreakSettings.podThreshold;
        if (!measure)
        {
            skin.Bar(new Rect(x, y, width, BarHeight), crystal,
                "BOR_PodCard_Crystallization".Translate(crystal.ToStringPercent(), needed.ToStringPercent()),
                crystal >= needed ? BorTone.Good : BorTone.Neutral);
        }
        y += BarHeight + Gap;

        Hediff? psychosis = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_TenebralDefOf.BOR_TenebralPsychosis);
        if (psychosis != null)
        {
            y = Line(x, y, width, "BOR_PodCard_Psychosis".Translate(psychosis.Severity.ToStringPercent()), skin.TextBad, measure);
        }

        // Tenebral implants already in place, then everything else that is wrong with them.
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        List<string> implants = hediffs.Where(h => ImplantDefs.Contains(h.def)).Select(Describe).ToList();
        y = Heading(x, y, width, "BOR_PodCard_Implants".Translate(), measure);
        y = Line(x, y, width, implants.Count > 0 ? implants.ToCommaList() : "BOR_PodCard_None".Translate().Resolve(), Color.white, measure);

        List<string> conditions = hediffs
            .Where(h => h.Visible && h.def.isBad && !ImplantDefs.Contains(h.def) && h.def != BOR_TenebralDefOf.BOR_TenebralPsychosis)
            .Select(Describe).Distinct().ToList();
        y = Heading(x, y, width, "BOR_PodCard_Conditions".Translate(), measure);
        string conditionText = conditions.Count == 0 ? "BOR_PodCard_None".Translate().Resolve()
            : conditions.Take(MaxConditions).ToCommaList()
              + (conditions.Count > MaxConditions ? " " + "BOR_PodCard_More".Translate(conditions.Count - MaxConditions).Resolve() : "");
        y = Line(x, y, width, conditionText, Color.white, measure);

        // Whether they can be picked now, and how they would get there.
        if (!arrival.NullOrEmpty() || blocked != null)
        {
            y += 4f;
            if (!measure)
            {
                skin.Fill(new Rect(x, y, width, 1f), new Color(1f, 1f, 1f, 0.1f));
            }
            y += Gap;
        }
        if (blocked != null)
        {
            y = Line(x, y, width, blocked, skin.TextBad, measure);
        }
        if (!arrival.NullOrEmpty())
        {
            y = Line(x, y, width, arrival, skin.TextDim, measure);
        }
        return y - rect.y + Pad - 2f;
    }

    private static string Who(Pawn pawn, string role)
    {
        string who = role + " · " + "BOR_PodCard_Age".Translate(pawn.ageTracker.AgeBiologicalYears);
        if (ModsConfig.BiotechActive && pawn.genes?.Xenotype != null && pawn.genes.Xenotype != XenotypeDefOf.Baseliner)
        {
            who += " · " + pawn.genes.XenotypeLabelCap;
        }
        return who;
    }

    private static string Describe(Hediff h) => h.Part == null ? h.LabelCap : h.LabelCap + " (" + h.Part.Label + ")";

    private static float Heading(float x, float y, float width, string text, bool measure)
    {
        y += 2f;
        Text.Font = GameFont.Tiny;
        if (!measure)
        {
            GUI.color = BorSkin.Current.TextDim;
            Widgets.Label(new Rect(x, y, width, 18f), text.ToUpper());
            GUI.color = Color.white;
        }
        Text.Font = GameFont.Small;
        return y + 17f;
    }

    // A wrapped line of small text.
    private static float Line(float x, float y, float width, string text, Color color, bool measure)
    {
        float h = Text.CalcHeight(text, width);
        if (!measure)
        {
            GUI.color = color;
            Widgets.Label(new Rect(x, y, width, h), text);
            GUI.color = Color.white;
        }
        return y + h + 2f;
    }
}
