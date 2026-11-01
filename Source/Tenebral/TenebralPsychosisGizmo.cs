using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Holds the player's work limit on the psychosis hediff and offers the gizmo for it.
public class HediffComp_PsychosisLimit : HediffComp
{
    // At this limit the pawn is never stopped, since severity tops out here.
    public const float NoLimit = 1f;

    public float limit = NoLimit;

    private Gizmo_TenebralPsychosis? gizmo;

    public bool LimitSet => limit < NoLimit;

    public bool Reached => LimitSet && parent.Severity >= limit;

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!parent.Visible || Pawn.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        yield return gizmo ??= new Gizmo_TenebralPsychosis(this);
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref limit, "psychosisLimit", NoLimit);
    }
}

// Psychosis level with a draggable work limit, like the psycast heat limit.
public class Gizmo_TenebralPsychosis : Gizmo_Slider
{
    private static readonly Color Bar = new(0.6f, 0.1f, 0.1f);
    private static readonly Color BarHighlight = new(0.75f, 0.15f, 0.15f);

    private static bool draggingBar;

    private readonly HediffComp_PsychosisLimit comp;

    public Gizmo_TenebralPsychosis(HediffComp_PsychosisLimit comp)
    {
        this.comp = comp;
    }

    protected override Color BarColor => Bar;

    protected override Color BarHighlightColor => BarHighlight;

    protected override bool IsDraggable => comp.Pawn.IsColonistPlayerControlled;

    protected override float ValuePercent => comp.parent.Severity;

    protected override float Target
    {
        get => comp.limit;
        set => comp.limit = value;
    }

    protected override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    protected override string BarLabel => $"{ValuePercent.ToStringPercent("0")} / {comp.limit.ToStringPercent("0")}";

    protected override string Title
    {
        get
        {
            string title = comp.parent.def.LabelCap;
            return Find.Selector.SelectedPawns.Count != 1 ? $"{title} ({comp.Pawn.LabelShort})" : title;
        }
    }

    protected override IEnumerable<float> GetBarThresholds()
    {
        foreach (HediffStage stage in comp.parent.def.stages)
        {
            if (stage.minSeverity > 0f)
            {
                yield return stage.minSeverity;
            }
        }
    }

    protected override string GetTooltip() => "BOR_PsychosisGizmoTip".Translate(
        comp.parent.Severity.ToStringPercent(), comp.parent.CurStage?.label ?? "", comp.limit.ToStringPercent());
}
