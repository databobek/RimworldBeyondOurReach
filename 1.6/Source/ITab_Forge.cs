using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

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
            float h = p.gridHeight * CellSize + (p.gridHeight - 1) * CellSpacing + Margin * 2f + HeaderHeight + FooterHeight + forge.PendingAnalysis.Count * PendingRowHeight;
            size = new Vector2(Mathf.Max(w, 240f) * WidthFactor, h);
        }
        base.UpdateSize();
    }

    protected override void FillTab()
    {
        CompForge? forge = Forge;
        if (forge == null)
        {
            return;
        }

        Rect inner = new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin);

        DrawHeader(new Rect(inner.x, inner.y, inner.width, 30f), forge);

        float gridTop = inner.y + HeaderHeight;
        int width = forge.Props.gridWidth;
        float gridWidthPx = width * CellSize + (width - 1) * CellSpacing;
        float startX = inner.x + (inner.width - gridWidthPx) / 2f;

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

        DrawFooter(new Rect(inner.x, inner.yMax - forge.PendingAnalysis.Count * PendingRowHeight, inner.width, forge.PendingAnalysis.Count * PendingRowHeight), forge);
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
            validator = info => info.HasThing && forge.CanAnalyze(info.Thing)
        };
        Find.Targeter.BeginTargeting(parms, target =>
        {
            Thing? thing = target.Thing;
            if (thing != null && forge.CanAnalyze(thing))
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

        Widgets.DefIcon(cell.ContractedBy(6f), def, forge.StuffOf(index));

        int count = forge.Queued(index);
        float progress = forge.Progress(index);
        if (count > 0 && progress > 0f)
        {
            float h = cell.height * Mathf.Clamp01(progress);
            Widgets.DrawBoxSolid(new Rect(cell.x, cell.yMax - h, cell.width, h), ProgressTint);
        }

        if (count > 0)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(cell.x, cell.y + 2f, cell.width - 4f, 30f), count.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        Widgets.DrawHighlightIfMouseover(cell);
        TooltipHandler.TipRegion(cell, () => TooltipFor(def), 7392000 + index);

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

    private static string TooltipFor(ThingDef def)
    {
        return "BOR_Forge_CellTip".Translate(def.LabelCap, ForgeMath.YieldFor(def)).Resolve();
    }

    private void DrawFooter(Rect rect, CompForge forge)
    {
        var pending = forge.PendingAnalysis.ToList();
        Text.Font = GameFont.Small;
        for (int i = 0; i < pending.Count; i++)
        {
            Thing t = pending[i];
            Rect row = new Rect(rect.x, rect.y + i * PendingRowHeight, rect.width, PendingRowHeight - 2f);
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
