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

public class ITab_Forge : BorTab
{
    private const float LargeGridHeight = 6f;

    // Big grids shrink their cells so the tab still fits on the screen.
    private float CellSize => Forge?.Props.gridHeight > LargeGridHeight ? 44f : 64f;

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

    private static readonly string?[] modeLabels = new string?[3];
    private static LoadedLanguage? modeLabelsLanguage;

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
            float h = p.gridHeight * CellSize + (p.gridHeight - 1) * CellSpacing + Margin * 2f + HeaderHeight + FooterHeight + (forge.PendingAnalysis.Count + (forge.IsFormatting ? 1 : 0)) * PendingRowHeight + ClearButtonHeight + 6f + TitleHeight;
            size = new Vector2(Mathf.Max(w, 240f) * WidthFactor + LeverWidth, h);
        }
        base.UpdateSize();
    }

    protected override void Contents(Rect rect)
    {
        CompForge? forge = Forge;
        if (forge == null)
        {
            selectingColumn = false;
            return;
        }

        Rect inner = rect.ContractedBy(Margin);

        Rect main = new Rect(inner.x, inner.y, inner.width - LeverWidth, inner.height);
        DrawHeader(new Rect(main.x, main.y, main.width, 30f), forge);

        float gridTop = inner.y + HeaderHeight;
        int width = forge.Props.gridWidth;
        float cellSize = CellSize;
        float gridWidthPx = width * cellSize + (width - 1) * CellSpacing;
        float startX = main.x + (main.width - gridWidthPx) / 2f;

        float rowsPx = forge.Props.gridHeight * cellSize + (forge.Props.gridHeight - 1) * CellSpacing;
        int hoverCol = selectingColumn ? HandleColumnSelect(forge, startX, gridTop, rowsPx, cellSize) : -1;

        for (int i = 0; i < forge.Capacity; i++)
        {
            int col = i % width;
            int row = i / width;
            Rect cell = new Rect(
                startX + col * (cellSize + CellSpacing),
                gridTop + row * (cellSize + CellSpacing),
                cellSize,
                cellSize);
            DrawCell(cell, forge, i);
        }

        if (hoverCol >= 0 && forge.FilledInColumn(hoverCol) > 0)
        {
            BorSkin.Current.Overlay(new Rect(startX + hoverCol * (cellSize + CellSpacing), gridTop, cellSize, rowsPx), FormatTint, BorTone.Bad);
        }

        DrawLever(new Rect(inner.xMax - LeverWidth + LeverPad, gridTop, LeverWidth - LeverPad, rowsPx + 6f + ClearButtonHeight), forge);

        Rect clearButton = new Rect(main.x + main.width * 0.125f, gridTop + rowsPx + 6f, main.width * 0.75f, ClearButtonHeight);
        string clearLabel = selectingColumn ? "CancelButton".Translate().Resolve() : "BOR_Forge_ClearColumn".Translate().Resolve();
        if (BorSkin.Current.Button(clearButton, clearLabel, tone: BorTone.Warn, selected: selectingColumn))
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
        BorSkin skin = BorSkin.Current;
        skin.Cell(new Rect(trackX - 3f, top, 6f, bottom - top), CellBack, CellBorder);

        int stages = CompForge.OverdriveStages;
        for (int i = 0; i < stages; i++)
        {
            float y = Mathf.Lerp(bottom, top, i / (float)(stages - 1));
            skin.Fill(new Rect(trackX - 8f, y - 0.5f, 16f, 1f), CellBorder);
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
            skin.Box(new Rect(trackX - 11f, currentY - 5f, 22f, 10f), CellBorder);
        }
        float handleY = Mathf.Lerp(bottom, top, forge.OverdriveTarget / (float)(stages - 1));
        Rect handle = new Rect(trackX - 11f, handleY - 5f, 22f, 10f);
        skin.Fill(handle, Color.Lerp(skin.ToneColor(BorTone.Good), skin.ToneColor(BorTone.Bad), forge.OverdriveTarget / (float)(stages - 1)));
        skin.Box(handle, Color.white);

        Text.Anchor = TextAnchor.MiddleCenter;
        if (forge.OverdrivePending)
        {
            GUI.color = skin.TextWarn;
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
    private int HandleColumnSelect(CompForge forge, float startX, float gridTop, float rowsPx, float cellSize)
    {
        int hover = -1;
        for (int c = 0; c < forge.Props.gridWidth; c++)
        {
            if (Mouse.IsOver(new Rect(startX + c * (cellSize + CellSpacing), gridTop, cellSize, rowsPx)))
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
        if (BorSkin.Current.Button(rect, label, active: !full, tone: BorTone.Info))
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

    private static string ShortCount(int n)
    {
        if (n < 1000)
        {
            return n.ToString();
        }
        int tenths = n / 100; // rounded down
        return tenths % 10 == 0 ? tenths / 10 + "k" : tenths / 10 + "." + tenths % 10 + "k";
    }

    private void DrawCell(Rect cell, CompForge forge, int index)
    {
        BorSkin skin = BorSkin.Current;
        skin.Cell(cell, CellBack, CellBorder);

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
            skin.Overlay(new Rect(cell.x, cell.yMax - h, cell.width, h), ProgressTint, BorTone.Good);
        }

        Text.Font = GameFont.Small;
        if (mode != ForgeMode.DoForever && count > 0)
        {
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(cell.x, cell.y + 2f, cell.width - 4f, 30f), ShortCount(count));
        }
        Text.Anchor = TextAnchor.LowerLeft;
        Widgets.Label(new Rect(cell.x + 4f, modeBar.y - 30f, cell.width - 4f, 30f), ShortCount(forge.CountOnMap(def)));
        Text.Anchor = TextAnchor.UpperLeft;

        DrawModeBar(modeBar, forge, index, mode);

        skin.Hover(cell);
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
        BorSkin.Current.ChipFace(bar);
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
        BorSkin.Current.Hover(bar);
        if (Mouse.IsOver(bar))
        {
            TooltipHandler.TipRegion(bar, "BOR_Forge_ModeTip".Translate());
        }
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

    // Every filled cell draws one of these on every GUI event. Cleared if the language changes while the game runs.
    private static string ModeLabel(ForgeMode mode)
    {
        if (modeLabelsLanguage != LanguageDatabase.activeLanguage)
        {
            modeLabelsLanguage = LanguageDatabase.activeLanguage;
            System.Array.Clear(modeLabels, 0, modeLabels.Length);
        }
        return modeLabels[(int)mode] ??= ("BOR_Forge_Mode_" + mode).Translate().Resolve();
    }

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
            BorSkin.Current.Bar(fRow, (forge.FormatDone + forge.FormatCellProgress) / total,
                "BOR_Forge_Formatting".Translate(forge.FormatColumn + 1, forge.FormatDone, total), BorTone.Warn);
            offset = 1;
        }
        for (int i = 0; i < pending.Count; i++)
        {
            Thing t = pending[i];
            Rect row = new Rect(rect.x, rect.y + (i + offset) * PendingRowHeight, rect.width, PendingRowHeight - 2f);
            Rect cancel = new Rect(row.xMax - row.height, row.y, row.height, row.height);
            Rect bar = new Rect(row.x, row.y, row.width - row.height - 4f, row.height);
            float prog = forge.AnalyzingThing == t ? forge.AnalysisProgress : 0f;
            BorSkin.Current.Bar(bar, prog, "BOR_Forge_Analyzing".Translate(t.LabelNoCount), BorTone.Info);
            if (BorSkin.Current.Button(cancel, "X", tone: BorTone.Bad))
            {
                forge.RemovePendingAnalysis(t);
                break;
            }
        }
    }
}
