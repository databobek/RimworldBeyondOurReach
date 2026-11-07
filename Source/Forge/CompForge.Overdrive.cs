using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public partial class CompForge
{
    public const int OverdriveStages = 10;
    private const float OverdriveYieldPerStage = 0.5f;
    private const float OverdrivePowerPerStage = 0.25f;

    private int overdriveStage;
    private int overdriveTarget;
    private float wearRemainder;
    private bool producing;
    private bool powerBoosted;
    private float wearPerTick;
    private float boostedOutput;
    private CompPowerTrader? powerTrader;

    // The stage the forge is actually running at; only a pawn's overdrive job changes it.
    public int OverdriveStage => overdriveStage;

    // The stage the player asked for on the lever.
    public int OverdriveTarget
    {
        get => overdriveTarget;
        set => overdriveTarget = Mathf.Clamp(value, 0, OverdriveStages - 1);
    }

    public bool OverdrivePending => overdriveTarget != overdriveStage;

    // Applies the requested stage. Any half-finished production run is discarded so a run cannot straddle two stages.
    public void ApplyOverdriveTarget()
    {
        if (!OverdrivePending)
        {
            return;
        }
        overdriveStage = overdriveTarget;
        wearRemainder = 0f;
        robotSlot = -1;
        robotTicks = 0;
        EnsureSize();
        for (int i = 0; i < progress.Length; i++)
        {
            progress[i] = 0f;
        }
        RefreshOverdriveCache();
        if (powerBoosted)
        {
            // Drop the stale boost; the next producing tick writes the new value.
            PowerTrader?.SetUpPowerVars();
            powerBoosted = false;
        }
    }

    // Displayed level: 1 at 100% .. 10 at 1000%.
    public int OverdriveLevel => overdriveStage + 1;

    public float YieldMultiplier(int stage) => 1f + OverdriveYieldPerStage * Props.stepMultiplier * stage;

    public float PowerMultiplier(int stage) => 1f + OverdrivePowerPerStage * Props.stepMultiplier * stage;

    // Fraction of max hitpoints lost per second of production; none at 100%.
    public float WearPerSecond(int stage) => Props.damagePerOverdriveStep / 100f * Props.stepMultiplier * stage;

    // Yield after overdrive, rounded down.
    public int OverdrivenYield(ThingDef def, int stage) => Mathf.FloorToInt(ForgeMath.YieldFor(def) * YieldMultiplier(stage));

    private CompPowerTrader? PowerTrader => powerTrader ??= parent.GetComp<CompPowerTrader>();

    // Everything that only changes with the stage (or on spawn) is computed here, not per tick.
    private void RefreshOverdriveCache()
    {
        wearPerTick = parent.MaxHitPoints * WearPerSecond(overdriveStage) / GenTicks.TicksPerRealSecond;
        CompPowerTrader? trader = PowerTrader;
        boostedOutput = trader == null || overdriveStage <= 0 ? 0f : -trader.Props.PowerConsumption * PowerMultiplier(overdriveStage);
    }

    // Call on every tick production work actually progresses (pawn or robot): applies overdrive wear.
    private int lastProducingTick = -999;

    // Lit while it produced in the last couple of ticks, by a pawn or on its own.
    public bool GlowActive => Find.TickManager.TicksGame - lastProducingTick <= 2;

    public void NotifyProducing()
    {
        producing = true;
        lastProducingTick = Find.TickManager.TicksGame;
        if (wearPerTick <= 0f)
        {
            return;
        }
        wearRemainder += wearPerTick;
        if (wearRemainder >= 1f)
        {
            int whole = Mathf.FloorToInt(wearRemainder);
            wearRemainder -= whole;
            parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, whole));
        }
    }

    // Writes the power output only when production starts or stops (or the stage changes), never every tick.
    private void UpdateOverdrivePower()
    {
        bool boost = producing && boostedOutput < 0f;
        producing = false;
        if (boost == powerBoosted)
        {
            return;
        }
        CompPowerTrader? trader = PowerTrader;
        if (trader == null)
        {
            return;
        }
        if (boost)
        {
            trader.PowerOutput = boostedOutput;
        }
        else
        {
            trader.SetUpPowerVars();
        }
        powerBoosted = boost;
    }
}
