using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

[StaticConstructorOnStartup]
public static class ForgeTextures
{
    public static readonly Texture2D TabAtlas = ContentFinder<Texture2D>.Get("UI/Widgets/TabAtlas");
}

public class ITab_Forge : ITab
{
    private const float CellSize = 64f;
    private const float CellSpacing = 6f;
    private const float Margin = 12f;
    private const float HeaderHeight = 34f;
    private const float FooterHeight = 12f;
    private const float PendingRowHeight = 26f;
    private const float WidthFactor = 1.25f;

    private static readonly Color CellBack = new(0.12f, 0.12f, 0.12f, 1f);
    private static readonly Color CellBorder = new(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color ProgressTint = new(0.45f, 0.45f, 0.45f, 0.65f);

    private const float ModeBarHeight = 14f;
    private const float ClearButtonHeight = 26f;

    private static readonly Color FormatTint = new(0.85f, 0.2f, 0.2f, 0.25f);

    private const float LeverWidth = 64f;
    private const float LeverPad = 6f;

    private bool selectingColumn;
    private bool draggingLever;

        public ITab_Forge()
    {
        labelKey = "BOR_TabForge";
        size = new Vector2(300f, 300f);
    }

    private CompForge? Forge => SelThing?.TryGetComp<CompForge>();

    public override bool IsVisible => Forge != null;

    protected override void UpdateSize()
    {
        CompForge? forge = Forge;
        if (forge != null)
        {
            CompProperties_Forge p = forge.Props;
            float w = p.gridWidth * CellSize + (p.gridWidth - 1) * CellSpacing + Margin * 2f;
            float h = p.gridHeight * CellSize + (p.gridHeight - 1) * CellSpacing + Margin * 2f + HeaderHeight + FooterHeight + (forge.PendingAnalysis.Count + (forge.IsFormatting ? 1 : 0)) * PendingRowHeight + ClearButtonHeight + 6f;
            size = new Vector2(Mathf.Max(w, 240f) * WidthFactor + LeverWidth, h);
        }
        base.UpdateSize();
    }

    protected override void FillTab()
    {
        CompForge? forge = Forge;
        if (forge == null)
        {
            selectingColumn = false;
            return;
        }

        Rect inner = new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin);

        Rect main = new Rect(inner.x, inner.y, inner.width - LeverWidth, inner.height);
        DrawHeader(new Rect(main.x, main.y, main.width, 30f), forge);

        float gridTop = inner.y + HeaderHeight;
        int width = forge.Props.gridWidth;
        float gridWidthPx = width * CellSize + (width - 1) * CellSpacing;
        float startX = main.x + (main.width - gridWidthPx) / 2f;

        float rowsPx = forge.Props.gridHeight * CellSize + (forge.Props.gridHeight - 1) * CellSpacing;
        int hoverCol = selectingColumn ? HandleColumnSelect(forge, startX, gridTop, rowsPx) : -1;

        for (int i = 0; i < forge.Capacity; i++)
        {
            int col = i % width;
            int row = i / width;
            Rect cell = new Rect(
                startX + col * (CellSize + CellSpacing),
                gridTop + row * (CellSize + CellSpacing),
                CellSize,
                CellSize);
            DrawCell(cell, forge, i);
        }

        if (hoverCol >= 0 && forge.FilledInColumn(hoverCol) > 0)
        {
            Widgets.DrawBoxSolid(new Rect(startX + hoverCol * (CellSize + CellSpacing), gridTop, CellSize, rowsPx), FormatTint);
        }

        DrawLever(new Rect(inner.xMax - LeverWidth + LeverPad, gridTop, LeverWidth - LeverPad, rowsPx + 6f + ClearButtonHeight), forge);

        Rect clearButton = new Rect(main.x + main.width * 0.125f, gridTop + rowsPx + 6f, main.width * 0.75f, ClearButtonHeight);
        string clearLabel = selectingColumn ? "CancelButton".Translate().Resolve() : "BOR_Forge_ClearColumn".Translate().Resolve();
        if (Widgets.ButtonText(clearButton, clearLabel))
        {
            selectingColumn = !selectingColumn;
        }
        TooltipHandler.TipRegion(clearButton, "BOR_Forge_ClearColumnTip".Translate());

        int footerRows = forge.PendingAnalysis.Count + (forge.IsFormatting ? 1 : 0);
        DrawFooter(new Rect(inner.x, inner.yMax - footerRows * PendingRowHeight, inner.width, footerRows * PendingRowHeight), forge);
    }

    // Vertical overdrive slider: OVERDRIVE written sideways on the left, a track with 10 stops on the right, current level underneath.
    private void DrawLever(Rect rect, CompForge forge)
    {
        float labelW = 22f;
        float trackH = rect.height - ClearButtonHeight - 6f;
        Rect label = new Rect(rect.x, rect.y, labelW, trackH);
        Rect slider = new Rect(rect.x + labelW, rect.y, rect.width - labelW, trackH);
        Rect readout = new Rect(rect.x, rect.yMax - ClearButtonHeight, rect.width, ClearButtonHeight);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        // Letters stacked top to bottom: a rotated label drew far outside its rect in the tab window.
        string word = "BOR_Forge_Overdrive".Translate();
        float lineH = Text.LineHeight - 4f;
        float letterY = label.center.y - word.Length * lineH / 2f;
        foreach (char c in word)
        {
            Widgets.Label(new Rect(label.x, letterY, label.width, lineH + 4f), c.ToString());
            letterY += lineH;
        }

        float trackX = slider.center.x;
        float top = slider.y + 8f;
        float bottom = slider.yMax - 8f;
        Widgets.DrawBoxSolid(new Rect(trackX - 3f, top, 6f, bottom - top), CellBack);
        GUI.color = CellBorder;
        Widgets.DrawBox(new Rect(trackX - 3f, top, 6f, bottom - top));
        GUI.color = Color.white;

        int stages = CompForge.OverdriveStages;
        for (int i = 0; i < stages; i++)
        {
            float y = Mathf.Lerp(bottom, top, i / (float)(stages - 1));
            Widgets.DrawBoxSolid(new Rect(trackX - 8f, y - 0.5f, 16f, 1f), CellBorder);
        }

        Rect hit = new Rect(slider.x, rect.y, slider.width, trackH);
        if (Event.current.type == EventType.MouseUp)
        {
            draggingLever = false;
        }
        if (Mouse.IsOver(hit) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            draggingLever = true;
        }
        if (draggingLever && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
        {
            float t = Mathf.InverseLerp(bottom, top, Event.current.mousePosition.y);
            int stage = Mathf.RoundToInt(t * (stages - 1));
            if (stage != forge.OverdriveTarget)
            {
                forge.OverdriveTarget = stage;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            Event.current.Use();
        }

        if (forge.OverdrivePending)
        {
            // Where the lever physically is right now, until a colonist moves it.
            float currentY = Mathf.Lerp(bottom, top, forge.OverdriveStage / (float)(stages - 1));
            GUI.color = CellBorder;
            Widgets.DrawBox(new Rect(trackX - 11f, currentY - 5f, 22f, 10f));
            GUI.color = Color.white;
        }
        float handleY = Mathf.Lerp(bottom, top, forge.OverdriveTarget / (float)(stages - 1));
        Rect handle = new Rect(trackX - 11f, handleY - 5f, 22f, 10f);
        Widgets.DrawBoxSolid(handle, Color.Lerp(new Color(0.35f, 0.75f, 0.35f), new Color(0.9f, 0.2f, 0.2f), forge.OverdriveTarget / (float)(stages - 1)));
        GUI.color = Color.white;
        Widgets.DrawBox(handle);

        Text.Anchor = TextAnchor.MiddleCenter;
        if (forge.OverdrivePending)
        {
            GUI.color = new Color(1f, 0.85f, 0.4f);
        }
        Widgets.Label(readout, (forge.OverdriveTarget + 1) * 100 + "%");
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        Rect whole = new Rect(rect.x, rect.y, rect.width, trackH + 6f + ClearButtonHeight);
        TooltipHandler.TipRegion(whole, () => LeverTip(forge), 7392100);
    }

    private static string LeverTip(CompForge forge)
    {
        int stage = forge.OverdriveTarget;
        string tip = "BOR_Forge_OverdriveTip".Translate(
            (stage + 1) * 100,
            forge.YieldMultiplier(stage).ToString("0.##"),
            Mathf.RoundToInt((forge.PowerMultiplier(stage) - 1f) * 100f),
            (forge.WearPerSecond(stage) * 100f).ToString("0.##")).Resolve();
        if (forge.OverdrivePending)
        {
            tip += "\n\n" + "BOR_Forge_OverdrivePending".Translate(forge.OverdriveLevel * 100).Resolve();
        }
        return tip;
    }

    // Returns the hovered column and consumes a click on it (right-click cancels selection).
    private int HandleColumnSelect(CompForge forge, float startX, float gridTop, float rowsPx)
    {
        int hover = -1;
        for (int c = 0; c < forge.Props.gridWidth; c++)
        {
            if (Mouse.IsOver(new Rect(startX + c * (CellSize + CellSpacing), gridTop, CellSize, rowsPx)))
            {
                hover = c;
            }
        }
        if (Event.current.type == EventType.MouseDown && hover >= 0)
        {
            if (Event.current.button == 0 && forge.FilledInColumn(hover) > 0)
            {
                forge.QueueFormat(hover);
                selectingColumn = false;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                Event.current.Use();
            }
            else if (Event.current.button == 1)
            {
                selectingColumn = false;
                Event.current.Use();
            }
        }
        return selectingColumn ? hover : -1;
    }

    private void DrawHeader(Rect rect, CompForge forge)
    {
        bool full = forge.FreeSlotCount <= 0;
        float newWidth = rect.width * 0.75f;
        rect = new Rect(rect.x + (rect.width - newWidth) / 2f, rect.y, newWidth, rect.height);
        string label = "BOR_Forge_Analyze".Translate();
        if (Widgets.ButtonText(rect, label, active: !full))
        {
            BeginAnalyzeTargeting(forge);
        }
        if (full)
        {
            TooltipHandler.TipRegion(rect, "BOR_Forge_GridFull".Translate());
        }
        else
        {
            TooltipHandler.TipRegion(rect, "BOR_Forge_AnalyzeTip".Translate());
        }
    }

    private static void BeginAnalyzeTargeting(CompForge forge)
    {
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        TargetingParameters parms = new()
        {
            canTargetItems = true,
            canTargetPawns = false,
            canTargetBuildings = false,
            mapObjectTargetsMustBeAutoAttackable = false,
            validator = info => info.HasThing && (forge.CanAnalyze(info.Thing) || (info.Thing.def.category == ThingCategory.Item && forge.IsExcluded(info.Thing.def)))
        };
        Find.Targeter.BeginTargeting(parms, target =>
        {
            Thing? thing = target.Thing;
            if (thing != null && forge.IsExcluded(thing.def))
            {
                Messages.Message("BOR_Forge_TooComplex".Translate(forge.parent.LabelCap), forge.parent, MessageTypeDefOf.RejectInput, historical: false);
            }
            else if (thing != null && forge.CanAnalyze(thing))
            {
                forge.AddPendingAnalysis(thing);
                Messages.Message("BOR_Forge_AnalysisQueued".Translate(thing.LabelNoCount), forge.parent, MessageTypeDefOf.SilentInput, historical: false);
            }
        });
    }

    private void DrawCell(Rect cell, CompForge forge, int index)
    {
        Widgets.DrawBoxSolid(cell, CellBack);
        GUI.color = CellBorder;
        Widgets.DrawBox(cell);
        GUI.color = Color.white;

        ThingDef? def = forge.Slots[index];
        if (def == null)
        {
            return;
        }

        Rect modeBar = new Rect(cell.x + 2f, cell.yMax - ModeBarHeight - 2f, cell.width - 4f, ModeBarHeight);
        Widgets.DefIcon(new Rect(cell.x + 6f, cell.y + 6f, cell.width - 12f, cell.height - 12f - ModeBarHeight), def, forge.StuffOf(index));

        ForgeMode mode = forge.ModeOf(index);
        int count = forge.Queued(index);
        float progress = forge.Progress(index);
        if (count > 0 && progress > 0f)
        {
            float h = cell.height * Mathf.Clamp01(progress);
            Widgets.DrawBoxSolid(new Rect(cell.x, cell.yMax - h, cell.width, h), ProgressTint);
        }

        Text.Font = GameFont.Small;
        if (mode != ForgeMode.DoForever && count > 0)
        {
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(cell.x, cell.y + 2f, cell.width - 4f, 30f), count.ToString());
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Widgets.Label(new Rect(cell.x + 4f, cell.y + 2f, cell.width - 4f, 30f), forge.CountOnMap(def).ToString());
        Text.Anchor = TextAnchor.UpperLeft;

        DrawModeBar(modeBar, forge, index, mode);

        Widgets.DrawHighlightIfMouseover(cell);
        TooltipHandler.TipRegion(cell, () => TooltipFor(def, forge), 7392000 + index);

        if (Mouse.IsOver(modeBar))
        {
            return;
        }

        bool shift = Event.current.shift;
        bool ctrl = Event.current.control || Event.current.command;
        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(cell))
        {
            int removeAmount = shift && ctrl ? 50 : ctrl ? 10 : shift ? 5 : 1;
            forge.RemoveQueued(index, removeAmount);
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            Event.current.Use();
            return;
        }
        if (Widgets.ButtonInvisible(cell))
        {
            int amount = shift && ctrl ? 50 : ctrl ? 10 : shift ? 5 : 1;
            forge.AddQueued(index, amount);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
    }

    private static void DrawModeBar(Rect bar, CompForge forge, int index, ForgeMode mode)
    {
        Widgets.DrawAtlas(bar, ForgeTextures.TabAtlas);
        Text.Font = GameFont.Tiny;
        int prevSize = Text.CurFontStyle.fontSize;
        bool prevWrap = Text.WordWrap;
        Text.CurFontStyle.fontSize = 9;
        Text.WordWrap = false;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(bar, ModeLabel(mode));
        Text.Anchor = TextAnchor.UpperLeft;
        Text.WordWrap = prevWrap;
        Text.CurFontStyle.fontSize = prevSize;
        Text.Font = GameFont.Small;
        Widgets.DrawHighlightIfMouseover(bar);
        TooltipHandler.TipRegion(bar, "BOR_Forge_ModeTip".Translate());
        if (Widgets.ButtonInvisible(bar))
        {
            List<FloatMenuOption> options = new();
            foreach (ForgeMode m in System.Enum.GetValues(typeof(ForgeMode)))
            {
                ForgeMode chosen = m;
                options.Add(new FloatMenuOption(ModeLabel(chosen), () => forge.SetMode(index, chosen)));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private static string ModeLabel(ForgeMode mode) => ("BOR_Forge_Mode_" + mode).Translate();

    private static string TooltipFor(ThingDef def, CompForge forge)
    {
        return "BOR_Forge_CellTip".Translate(def.LabelCap, forge.OverdrivenYield(def, forge.OverdriveStage)).Resolve();
    }

    private void DrawFooter(Rect rect, CompForge forge)
    {
        var pending = forge.PendingAnalysis.ToList();
        Text.Font = GameFont.Small;
        int offset = 0;
        if (forge.IsFormatting)
        {
            Rect fRow = new Rect(rect.x, rect.y, rect.width, PendingRowHeight - 2f);
            int total = System.Math.Max(1, forge.FormatTotal);
            Widgets.FillableBar(fRow, (forge.FormatDone + forge.FormatCellProgress) / total);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(fRow.x + 4f, fRow.y, fRow.width - 4f, fRow.height), "BOR_Forge_Formatting".Translate(forge.FormatColumn + 1, forge.FormatDone, total));
            Text.Anchor = TextAnchor.UpperLeft;
            offset = 1;
        }
        for (int i = 0; i < pending.Count; i++)
        {
            Thing t = pending[i];
            Rect row = new Rect(rect.x, rect.y + (i + offset) * PendingRowHeight, rect.width, PendingRowHeight - 2f);
            Rect cancel = new Rect(row.xMax - row.height, row.y, row.height, row.height);
            Rect bar = new Rect(row.x, row.y, row.width - row.height - 4f, row.height);
            float prog = forge.AnalyzingThing == t ? forge.AnalysisProgress : 0f;
            Widgets.FillableBar(bar, prog);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(bar.x + 4f, bar.y, bar.width - 4f, bar.height), "BOR_Forge_Analyzing".Translate(t.LabelNoCount));
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonText(cancel, "X"))
            {
                forge.RemovePendingAnalysis(t);
                break;
            }
        }
    }
}
