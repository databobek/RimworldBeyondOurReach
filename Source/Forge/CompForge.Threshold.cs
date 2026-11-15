using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public partial class CompForge
{
    public const int MaxNeutroniumThreshold = 100000;

    // Production runs only start while the net holds at least this much. 0 = no limit.
    private int neutroniumThreshold;

    private Gizmo_ForgeThreshold? thresholdGizmo;

    public bool UsesPipeResource => Props.pipeNet != null && Props.pipeAmount > 0f;

    public int NeutroniumThreshold
    {
        get => neutroniumThreshold;
        set => neutroniumThreshold = Mathf.Clamp(value, 0, MaxNeutroniumThreshold);
    }

    public float NetStored => PipeComp?.PipeNet?.Stored ?? 0f;

    public bool ThresholdMet => !UsesPipeResource || neutroniumThreshold <= 0 || NetStored >= neutroniumThreshold;

    // Checked when a run starts, never mid-run.
    public bool CanStartProduction => HasResource && ThresholdMet;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (UsesPipeResource && parent.Faction == Faction.OfPlayer)
        {
            yield return thresholdGizmo ??= new Gizmo_ForgeThreshold(this);
        }
    }
}

// Net neutronium against this forge's start threshold, dragged like the psycast heat limit.
public class Gizmo_ForgeThreshold : Gizmo_Slider
{
    private static readonly Color Bar = new(0.95f, 0.95f, 0.02f);
    private static readonly Color BarHighlight = new(1f, 1f, 0.4f);

    private static bool draggingBar;

    private readonly CompForge comp;

    public Gizmo_ForgeThreshold(CompForge comp)
    {
        this.comp = comp;
    }

    protected override Color BarColor => Bar;

    protected override Color BarHighlightColor => BarHighlight;

    protected override bool IsDraggable => comp.parent.Faction == Faction.OfPlayer;

    // Steps of 1 000 over 0..100 000.
    protected override int Increments => 100;

    protected override float ValuePercent => Mathf.Clamp01(comp.NetStored / CompForge.MaxNeutroniumThreshold);

    protected override float Target
    {
        get => (float)comp.NeutroniumThreshold / CompForge.MaxNeutroniumThreshold;
        set => comp.NeutroniumThreshold = Mathf.RoundToInt(value * CompForge.MaxNeutroniumThreshold);
    }

    protected override bool DraggingBar
    {
        get => draggingBar;
        set => draggingBar = value;
    }

    protected override string BarLabel => $"{comp.NetStored:N0} / {(comp.NeutroniumThreshold > 0 ? comp.NeutroniumThreshold.ToString("N0") : "-")}";

    protected override string Title
    {
        get
        {
            string title = "BOR_Forge_Threshold".Translate();
            return Find.Selector.NumSelected > 1 ? $"{title} ({comp.parent.LabelShort})" : title;
        }
    }

    protected override string GetTooltip() => "BOR_Forge_ThresholdTip".Translate(
        comp.NetStored.ToString("N0"),
        comp.NeutroniumThreshold > 0 ? comp.NeutroniumThreshold.ToString("N0") : "BOR_Forge_ThresholdNone".Translate().ToString());
}
