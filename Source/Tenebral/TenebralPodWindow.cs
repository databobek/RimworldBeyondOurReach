using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Control window of the Tenebral Pod: action, pawn (colonists, slaves, prisoners), then for an operation the organ
// or appendage and where to install it.
public class Window_TenebralPod : BorWindow
{
    private const float RowHeight = 28f;
    private const float Gap = 10f;

    private sealed class Option
    {
        public RecipeDef Recipe = null!;
        public List<BodyPartRecord> Parts = new();
        public string? Locked;
        public bool Organ;
    }

    private static readonly TenebralPodMode[] Actions =
    {
        TenebralPodMode.Transformation, TenebralPodMode.Healing, TenebralPodMode.Stasis, TenebralPodMode.Operating
    };

    private readonly Building_TenebralPod pod;
    private readonly List<Pawn> colonists, slaves, prisoners;
    private TenebralPodMode action;
    private Pawn? pawn;
    private List<Option> options = new();
    private Option? option;
    private BodyPartRecord? part;
    private Vector2 colonistScroll, slaveScroll, prisonerScroll, organScroll, appendageScroll, partScroll;

    public override Vector2 InitialSize => new(1040f, 720f);

    public Window_TenebralPod(Building_TenebralPod pod)
    {
        this.pod = pod;
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
        MapPawns pawns = pod.Map.mapPawns;
        colonists = pawns.FreeColonistsSpawned.Where(p => p.IsFreeNonSlaveColonist && p.RaceProps.Humanlike).ToList();
        slaves = pawns.SlavesOfColonySpawned.Where(p => p.RaceProps.Humanlike).ToList();
        prisoners = pawns.PrisonersOfColonySpawned.Where(p => p.RaceProps.Humanlike).ToList();
        Pawn? inside = pod.Occupant;
        if (inside != null)
        {
            (inside.IsPrisonerOfColony ? prisoners : inside.IsSlaveOfColony ? slaves : colonists).Add(inside);
        }
        action = pod.Mode;
        Pawn? start = pod.OperationTarget ?? inside;
        if (start != null && Eligible(start) == null)
        {
            SelectPawn(start);
        }
    }

    private bool Colonist(Pawn p) => colonists.Contains(p);

    // Null when the pawn can be chosen for the current action, otherwise why not.
    private string? Eligible(Pawn p)
    {
        if ((action == TenebralPodMode.Transformation || action == TenebralPodMode.Operating) && !Colonist(p))
        {
            return "BOR_PodColonistsOnly".Translate();
        }
        AcceptanceReport report = pod.CanAcceptFor(p, action);
        return report.Accepted ? null : report.Reason;
    }

    private void SelectAction(TenebralPodMode m)
    {
        action = m;
        option = null;
        part = null;
        if (pawn != null && Eligible(pawn) != null)
        {
            pawn = null;
            options = new List<Option>();
        }
        else if (pawn != null)
        {
            SelectPawn(pawn);
        }
    }

    private void SelectPawn(Pawn p)
    {
        pawn = p;
        option = null;
        part = null;
        options = new List<Option>();
        if (action != TenebralPodMode.Operating)
        {
            return;
        }
        foreach (RecipeDef recipe in Building_TenebralPod.Operations)
        {
            List<BodyPartRecord> parts = recipe.Worker.GetPartsToApplyOn(p, recipe).ToList();
            if (parts.Count == 0)
            {
                continue;
            }
            options.Add(new Option
            {
                Recipe = recipe,
                Parts = parts,
                Organ = recipe.ingredients.Any(i => i.filter.Allows(BOR_TenebralDefOf.BOR_TenebralOrgan)),
                Locked = recipe.AvailableNow ? null : "BOR_TenebralPodNeedsResearch".Translate(Building_TenebralPod.PrerequisitesLabel(recipe)).Resolve()
            });
        }
    }

    private void SelectOption(Option o)
    {
        option = o;
        part = o.Parts.Count == 1 ? o.Parts[0] : null;
    }

    private bool HasEnough(out List<string> lines)
    {
        lines = new List<string>();
        bool ok = true;
        if (option == null)
        {
            return false;
        }
        foreach (ThingDefCountClass r in Building_TenebralPod.Requirements(option.Recipe))
        {
            int have = pod.ColonyCount(r.thingDef);
            bool enough = have >= r.count;
            ok &= enough;
            string line = r.thingDef.LabelCap + " " + have + " / " + r.count;
            lines.Add(enough ? line : line.Colorize(ColorLibrary.RedReadable));
        }
        return ok;
    }

    private string ArrivalHint() => ArrivalHint(pawn);

    private string ArrivalHint(Pawn? p)
    {
        if (p == null || p == pod.Occupant)
        {
            return "";
        }
        bool carried = p.IsPrisonerOfColony || p.Downed || !p.health.capacities.CapableOf(PawnCapacityDefOf.Moving);
        return (carried ? "BOR_PodCarried" : "BOR_PodWalks").Translate(p.LabelShortCap);
    }

    private void ShowCard(Pawn p, string role)
    {
        string? blocked = Eligible(p);
        if (blocked == null && pod.PodBusy(p))
        {
            blocked = "BOR_PodOccupied".Translate();
        }
        TenebralPodPawnCard.Show(p, role, blocked?.CapitalizeFirst(), ArrivalHint(p));
    }

    protected override string Title => "BOR_TenebralPodTitle".Translate();

    protected override BorUISchemeDef Scheme => BOR_UISchemeDefOf.BOR_UIScheme_Tenebral;

    protected override void Contents(Rect inRect)
    {
        const float footer = 150f;
        float top = inRect.y;
        float height = inRect.height - footer;
        float actionWidth = 170f;
        float pawnWidth = 250f;
        float columnWidth = (inRect.width - actionWidth - pawnWidth - Gap * 3f) / 2f;

        // Action
        List<(string, bool, string?, Action)> actionRows = new();
        foreach (TenebralPodMode m in Actions)
        {
            TenebralPodMode chosen = m;
            actionRows.Add((("BOR_TenebralPodMode_" + m).Translate().CapitalizeFirst(), m == action, null, () => SelectAction(chosen)));
        }
        Vector2 none = Vector2.zero;
        DrawList(new Rect(inRect.x, top, actionWidth, height), "BOR_PodAction".Translate(), ref none, actionRows);

        // Pawns: three stacked lists
        float px = inRect.x + actionWidth + Gap;
        float section = (height - Gap * 2f) / 3f;
        // Hovering a pawn shows their card (TenebralPodPawnCard) next to the mouse.
        int hoverColonist = DrawList(new Rect(px, top, pawnWidth, section), "BOR_PodColonists".Translate(), ref colonistScroll, PawnRows(colonists));
        int hoverSlave = DrawList(new Rect(px, top + section + Gap, pawnWidth, section), "BOR_PodSlaves".Translate(), ref slaveScroll, PawnRows(slaves));
        int hoverPrisoner = DrawList(new Rect(px, top + (section + Gap) * 2f, pawnWidth, section), "BOR_PodPrisoners".Translate(), ref prisonerScroll, PawnRows(prisoners));
        if (hoverColonist >= 0)
        {
            ShowCard(colonists[hoverColonist], "BOR_PodColonist".Translate());
        }
        else if (hoverSlave >= 0)
        {
            ShowCard(slaves[hoverSlave], "BOR_PodSlave".Translate());
        }
        else if (hoverPrisoner >= 0)
        {
            ShowCard(prisoners[hoverPrisoner], "BOR_PodPrisoner".Translate());
        }

        // Organs and appendages, only for an operation
        float ox = px + pawnWidth + Gap;
        bool operating = action == TenebralPodMode.Operating;
        DrawList(new Rect(ox, top, columnWidth, height), "BOR_OpOrgans".Translate(), ref organScroll, OptionRows(true), disabled: !operating);
        DrawList(new Rect(ox + columnWidth + Gap, top, columnWidth, height), "BOR_OpAppendages".Translate(), ref appendageScroll, OptionRows(false), disabled: !operating);

        float y = top + height + Gap;
        bool enough = true;
        if (operating)
        {
            DrawParts(new Rect(inRect.x, y, inRect.width, 50f));
            enough = HasEnough(out List<string> lines);
            Rect costRect = new(inRect.x, y + 56f, inRect.width, 28f);
            string costPlain = "BOR_OpCost".Translate() + " " + (option == null ? "-" : lines.ToCommaList());
            TooltipHandler.TipRegion(costRect, costPlain.StripTags());
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(costRect, costPlain);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        Rect hintRect = new(inRect.x, y + 86f, inRect.width, 24f);
        Widgets.Label(hintRect, Fit(hintRect, ArrivalHint()));

        string? blocked = pawn == null ? "BOR_OpPickColonist".Translate().Resolve()
            : pod.PodBusy(pawn) ? "BOR_PodOccupied".Translate().Resolve()
            : operating && option == null ? "BOR_OpPickOperation".Translate().Resolve()
            : operating && part == null ? "BOR_OpPickPart".Translate().Resolve()
            : operating && !enough ? "BOR_OpMissingItems".Translate().Resolve()
            : null;
        Rect accept = new(inRect.x, inRect.yMax - 36f, 160f, 36f);
        Rect cancel = new(inRect.xMax - 160f, inRect.yMax - 36f, 160f, 36f);
        if (BorSkin.Current.Button(accept, "AcceptButton".Translate(), active: blocked == null, tone: BorTone.Good))
        {
            string? failed = pod.Order(pawn!, action, option?.Recipe, part);
            if (failed != null)
            {
                Messages.Message(failed, pod, MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                Close();
            }
        }
        if (blocked != null)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect hint = new(accept.xMax + Gap, accept.y, cancel.x - accept.xMax - Gap * 2f, 36f);
            Widgets.Label(hint, Fit(hint, blocked).Colorize(ColoredText.SubtleGrayColor));
            Text.Anchor = TextAnchor.UpperLeft;
        }
        if (BorSkin.Current.Button(cancel, "CancelButton".Translate()))
        {
            Close();
        }
    }

    private List<(string, bool, string?, Action)> PawnRows(List<Pawn> list)
    {
        List<(string, bool, string?, Action)> rows = new();
        foreach (Pawn p in list)
        {
            Pawn captured = p;
            rows.Add((p.LabelShortCap, p == pawn, Eligible(p), () => SelectPawn(captured)));
        }
        return rows;
    }

    private List<(string, bool, string?, Action)> OptionRows(bool organs)
    {
        List<(string, bool, string?, Action)> rows = new();
        foreach (Option o in options.Where(o => o.Organ == organs))
        {
            Option captured = o;
            rows.Add((o.Recipe.LabelCap.Resolve(), o == option, o.Locked, () => SelectOption(captured)));
        }
        return rows;
    }

    // Cuts text that does not fit with an ellipsis and puts the whole text in a tooltip.
    private static string Fit(Rect rect, string text, float padding = 0f)
    {
        string cut = text.Truncate(rect.width - padding);
        if (cut != text)
        {
            TooltipHandler.TipRegion(rect, text);
        }
        return cut;
    }

    // Returns the row under the mouse, or -1.
    private static int DrawList(Rect rect, string title, ref Vector2 scroll, List<(string label, bool selected, string? locked, Action click)> rows, bool disabled = false)
    {
        int hovered = -1;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 24f), title);
        Rect outer = new(rect.x, rect.y + 26f, rect.width, rect.height - 26f);
        BorSkin skin = BorSkin.Current;
        skin.Section(outer);
        if (disabled)
        {
            skin.Fill(outer, new Color(0f, 0f, 0f, 0.35f));
            rows = new List<(string, bool, string?, Action)>();
        }
        Rect view = new(0f, 0f, outer.width - 16f, Mathf.Max(outer.height, rows.Count * RowHeight));
        Widgets.BeginScrollView(outer, ref scroll, view);
        for (int i = 0; i < rows.Count; i++)
        {
            (string label, bool selected, string? locked, Action click) = rows[i];
            Rect row = new(0f, i * RowHeight, view.width, RowHeight);
            if (Mouse.IsOver(row))
            {
                hovered = i;
            }
            skin.Row(row, selected, hoverable: locked == null);
            Text.Anchor = TextAnchor.MiddleLeft;
            string full = locked == null ? label : label + " (" + locked + ")";
            string text = Fit(row, full, 12f);
            Widgets.Label(new Rect(row.x + 6f, row.y, row.width - 6f, row.height), locked == null ? text : text.Colorize(ColoredText.SubtleGrayColor));
            Text.Anchor = TextAnchor.UpperLeft;
            if (locked == null && Widgets.ButtonInvisible(row))
            {
                click();
            }
        }
        Widgets.EndScrollView();
        return hovered;
    }

    private void DrawParts(Rect rect)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 22f), "BOR_OpInstallAt".Translate());
        Rect area = new(rect.x, rect.y + 24f, rect.width, rect.height - 24f);
        if (option == null)
        {
            return;
        }
        Rect view = new(0f, 0f, Mathf.Max(area.width, option.Parts.Count * 150f), area.height - 16f);
        Widgets.BeginScrollView(area, ref partScroll, view);
        float x = 0f;
        foreach (BodyPartRecord p in option.Parts)
        {
            Rect button = new(x, 0f, 144f, 26f);
            if (BorSkin.Current.Button(button, Fit(button, p.LabelCap, 10f), selected: p == part))
            {
                part = p;
            }
            x += 150f;
        }
        Widgets.EndScrollView();
    }
}
