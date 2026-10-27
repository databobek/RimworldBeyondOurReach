using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class CompProperties_Forge : CompProperties
{
    public int gridWidth = 2;
    public int gridHeight = 4;

    public PipeNetDef? pipeNet;
    public float pipeAmount;

    // Defnames or trade tags this forge refuses to analyze.
    public List<string> exclusionList = new();

    // Percent bonus applied to the analysis success chance (50 => x1.5).
    public float analysisSuccessOffset;
    public int workTicksPerDraw = 10;

    // Percent of max hitpoints lost per second for each overdrive step above 100% (1 => +1% per second per step).
    public float damagePerOverdriveStep = 0.1f;

    // Scales what each overdrive step adds: output, power use and wear per step are all multiplied by this.
    public float stepMultiplier = 1f;

    public CompProperties_Forge()
    {
        compClass = typeof(CompForge);
    }

    public int Capacity => gridWidth * gridHeight;
}

public enum ForgeMode
{
    DoX,
    DoUntil,
    DoForever
}

public class CompForge : ThingComp
{
    public const int FormatTicksPerCell = GenDate.TicksPerHour;

    // EvenMoreLinkables' Robot Assistant is identified by its work-helper comp, resolved by reflection (soft dependency).
    private const string WorkHelperType = "Hobbes.MoreLinkablesProduction.CompWorkHelper";
    private const float AutomationForgeSpeed = 1.5f;

    private static readonly System.Type? WorkHelperComp = GenTypes.GetTypeInAnyAssembly(WorkHelperType);
    private static readonly System.Reflection.FieldInfo? WorkHelperState = WorkHelperComp?.GetField("state");

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

    private int robotSlot = -1;
    private int robotTicks;
    private List<int> modes = new();
    private List<int> formatQueue = new();
    private int formatTicks;
    private int formatDone;
    private List<ThingDef?> slots = new();
    private List<int> queued = new();
    private List<ThingDef?> slotStuff = new();
    private List<Thing> pendingAnalysis = new();

    private float[] progress = System.Array.Empty<float>();

    public CompProperties_Forge Props => (CompProperties_Forge)props;

    public int Capacity => Props.Capacity;

    public IReadOnlyList<ThingDef?> Slots
    {
        get
        {
            EnsureSize();
            return slots;
        }
    }

    public IReadOnlyList<Thing> PendingAnalysis
    {
        get
        {
            pendingAnalysis.RemoveAll(t => t == null || t.Destroyed);
            return pendingAnalysis;
        }
    }

    public int FreeSlotCount => slots.Count(s => s == null) - PendingAnalysis.Count;

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

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        RefreshOverdriveCache();
    }

    // Call on every tick production work actually progresses (pawn or robot): applies overdrive wear.
    public void NotifyProducing()
    {
        producing = true;
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

    private void EnsureSize()
    {
        int cap = Capacity;
        while (slots.Count < cap)
        {
            slots.Add(null);
        }
        while (slotStuff.Count < cap)
        {
            slotStuff.Add(null);
        }
        while (modes.Count < cap)
        {
            modes.Add(0);
        }
        while (queued.Count < cap)
        {
            queued.Add(0);
        }
        if (progress.Length != cap)
        {
            progress = new float[cap];
        }
    }

    public bool Knows(ThingDef def) => def != null && Slots.Contains(def);

    public int Queued(int index)
    {
        EnsureSize();
        return queued[index];
    }

    public float Progress(int index)
    {
        EnsureSize();
        return progress[index];
    }

    public void SetProgress(int index, float value)
    {
        EnsureSize();
        progress[index] = value;
    }

    public bool IsFormatting
    {
        get
        {
            PruneFormatQueue();
            return formatQueue.Count > 0;
        }
    }

    public int FormatColumn => IsFormatting ? formatQueue[0] : -1;

    public int FormatDone => formatDone;

    public int FormatTotal => formatDone + FilledInColumn(FormatColumn);

    public float FormatCellProgress => (float)formatTicks / FormatTicksPerCell;

    public int FilledInColumn(int col)
    {
        EnsureSize();
        int width = Props.gridWidth;
        int n = 0;
        for (int i = col; col >= 0 && i < slots.Count; i += width)
        {
            if (slots[i] != null)
            {
                n++;
            }
        }
        return n;
    }

    private void PruneFormatQueue()
    {
        while (formatQueue.Count > 0 && FilledInColumn(formatQueue[0]) == 0)
        {
            formatQueue.RemoveAt(0);
            formatTicks = 0;
            formatDone = 0;
        }
    }

    public void QueueFormat(int col)
    {
        if (FilledInColumn(col) > 0 && !formatQueue.Contains(col))
        {
            formatQueue.Add(col);
        }
    }

    // Advances formatting by one tick; each full hour wipes the topmost filled cell of the active column.
    public void FormatTick()
    {
        if (!IsFormatting)
        {
            return;
        }
        formatTicks++;
        if (formatTicks < FormatTicksPerCell)
        {
            return;
        }
        formatTicks = 0;
        int width = Props.gridWidth;
        for (int i = formatQueue[0]; i < slots.Count; i += width)
        {
            if (slots[i] != null)
            {
                slots[i] = null;
                slotStuff[i] = null;
                queued[i] = 0;
                modes[i] = 0;
                progress[i] = 0f;
                formatDone++;
                break;
            }
        }
        PruneFormatQueue();
    }

    // A powered, switched-on work helper in WORK mode (the automatic mode) works linked forges without a pawn.
    private static bool IsAutomating(Thing facility)
    {
        if (WorkHelperComp == null)
        {
            return false;
        }
        ThingComp? helper = (facility as ThingWithComps)?.AllComps.FirstOrDefault(c => c.GetType() == WorkHelperComp);
        if (helper == null || WorkHelperState?.GetValue(helper)?.ToString() != "WORK")
        {
            return false;
        }
        return (facility.TryGetComp<CompPowerTrader>()?.PowerOn ?? true)
            && (facility.TryGetComp<CompFlickable>()?.SwitchIsOn ?? true);
    }

    private float AutomationSpeed
    {
        get
        {
            if (WorkHelperComp == null || !PowerOn || parent.Map == null)
            {
                return 0f;
            }
            List<Thing>? linked = parent.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading;
            return linked != null && linked.Any(IsAutomating) ? AutomationForgeSpeed : 0f;
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned)
        {
            return;
        }
        UpdateOverdrivePower();
        float speed = AutomationSpeed;
        if (speed <= 0f)
        {
            return;
        }
        // A pawn working the forge takes precedence.
        if (parent.Map.reservationManager.IsReservedByAnyoneOf(parent, Faction.OfPlayer))
        {
            return;
        }
        if (IsFormatting)
        {
            FormatTick();
            return;
        }
        if (PendingAnalysis.Count > 0)
        {
            return;
        }
        int slot = robotSlot >= 0 && robotSlot < Capacity && ShouldProduce(robotSlot) ? robotSlot : NextProductionSlot();
        if (slot < 0)
        {
            robotSlot = -1;
            robotTicks = 0;
            return;
        }
        if (slot != robotSlot)
        {
            robotSlot = slot;
            robotTicks = 0;
        }
        if (robotTicks % Props.workTicksPerDraw == 0 && !TryDrawResource())
        {
            return;
        }
        ThingDef def = slots[slot]!;
        int total = ForgeMath.RobotProductionTicks(def, speed);
        robotTicks++;
        NotifyProducing();
        SetProgress(slot, (float)robotTicks / total);
        if (robotTicks >= total)
        {
            robotTicks = 0;
            SetProgress(slot, 0f);
            FinishProduction(slot);
        }
    }

    public void FinishProduction(int index)
    {
        EnsureSize();
        ThingDef? def = slots[index];
        if (def == null)
        {
            return;
        }
        ThingDef? stuff = def.MadeFromStuff ? (slotStuff[index] ?? GenStuff.DefaultStuffFor(def)) : null;
        int remaining = OverdrivenYield(def, overdriveStage);
        ConsumeQueued(index);
        IntVec3 cell = parent.InteractionCell;
        Map map = parent.Map;
        while (remaining > 0)
        {
            Thing product = ThingMaker.MakeThing(def, stuff);
            product.stackCount = Mathf.Min(remaining, def.stackLimit);
            remaining -= product.stackCount;
            product.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            GenPlace.TryPlaceThing(product, cell, map, ThingPlaceMode.Near);
        }
    }

    public ForgeMode ModeOf(int index)
    {
        EnsureSize();
        return (ForgeMode)modes[index];
    }

    public void SetMode(int index, ForgeMode mode)
    {
        EnsureSize();
        modes[index] = (int)mode;
    }

    public int CountOnMap(ThingDef def)
    {
        Map? map = parent.Map;
        if (map == null)
        {
            return 0;
        }
        int total = 0;
        foreach (Thing t in map.listerThings.ThingsOfDef(def))
        {
            total += t.stackCount;
        }
        return total;
    }

    public bool ShouldProduce(int index)
    {
        EnsureSize();
        ThingDef? def = slots[index];
        if (def == null)
        {
            return false;
        }
        switch ((ForgeMode)modes[index])
        {
            case ForgeMode.DoForever:
                return true;
            case ForgeMode.DoUntil:
                return queued[index] > 0 && CountOnMap(def) < queued[index];
            default:
                return queued[index] > 0;
        }
    }

    public void AddQueued(int index, int amount)
    {
        EnsureSize();
        if (slots[index] != null)
        {
            queued[index] += amount;
        }
    }

    public void ConsumeQueued(int index)
    {
        EnsureSize();
        if (modes[index] != (int)ForgeMode.DoX)
        {
            return;
        }
        queued[index] = System.Math.Max(0, queued[index] - 1);
    }

    public int NextProductionSlot()
    {
        EnsureSize();
        for (int i = 0; i < slots.Count; i++)
        {
            if (ShouldProduce(i))
            {
                return i;
            }
        }
        return -1;
    }

    public bool IsExcluded(ThingDef def)
    {
        List<string> list = Props.exclusionList;
        if (def == null || list == null || list.Count == 0)
        {
            return false;
        }
        if (list.Contains(def.defName))
        {
            return true;
        }
        if (def.tradeTags != null)
        {
            foreach (string tag in def.tradeTags)
            {
                if (list.Contains(tag))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool CanAnalyze(Thing thing)
    {
        if (thing == null || thing.Destroyed || FreeSlotCount <= 0)
        {
            return false;
        }
        if (thing.def.category != ThingCategory.Item || thing is Corpse || thing is MinifiedThing)
        {
            return false;
        }
        if (!thing.def.EverHaulable || thing.def.BaseMarketValue <= 0f || thing.def.isUnfinishedThing)
        {
            return false;
        }
        if (thing.def.defName == "BOR_Neutronium")
        {
            return false;
        }
        if (Knows(thing.def) || PendingAnalysis.Any(t => t.def == thing.def))
        {
            return false;
        }
        return true;
    }

    public void AddPendingAnalysis(Thing thing)
    {
        if (!pendingAnalysis.Contains(thing))
        {
            pendingAnalysis.Add(thing);
        }
    }

    public Thing? AnalyzingThing;
    public float AnalysisProgress;

    public void RemoveQueued(int index, int amount)
    {
        EnsureSize();
        queued[index] = System.Math.Max(0, queued[index] - amount);
        if (queued[index] == 0)
        {
            progress[index] = 0f;
        }
    }

    public void RemovePendingAnalysis(Thing thing)
    {
        pendingAnalysis.Remove(thing);
        if (AnalyzingThing == thing)
        {
            AnalyzingThing = null;
            AnalysisProgress = 0f;
        }
    }

    public ThingDef? StuffOf(int index)
    {
        EnsureSize();
        return slotStuff[index];
    }

    public void Learn(ThingDef def, ThingDef? stuff = null)
    {
        EnsureSize();
        if (Knows(def))
        {
            return;
        }
        int index = slots.IndexOf(null);
        if (index >= 0)
        {
            slots[index] = def;
            slotStuff[index] = def.MadeFromStuff ? stuff : null;
        }
    }

    private CompResource? PipeComp =>
        Props.pipeNet == null
            ? null
            : parent.AllComps.OfType<CompResource>().FirstOrDefault(c => c.Props.pipeNet == Props.pipeNet);

    public bool HasResource
    {
        get
        {
            if (Props.pipeNet == null || Props.pipeAmount <= 0f)
            {
                return true;
            }
            PipeNet? net = PipeComp?.PipeNet;
            return net != null && net.Stored >= Props.pipeAmount;
        }
    }

    public bool TryDrawResource()
    {
        if (Props.pipeNet == null || Props.pipeAmount <= 0f)
        {
            return true;
        }
        PipeNet? net = PipeComp?.PipeNet;
        if (net == null || net.Stored < Props.pipeAmount)
        {
            return false;
        }
        net.DrawAmongStorage(Props.pipeAmount, net.storages);
        return true;
    }

    public bool PowerOn => parent.GetComp<CompPowerTrader>()?.PowerOn ?? true;

    public bool IsAssignedOrUnassigned(Pawn pawn)
    {
        CompAssignableToPawn? assign = parent.GetComp<CompAssignableToPawn>();
        if (assign == null || assign.AssignedPawnsForReading.Count == 0)
        {
            return true;
        }
        return assign.AssignedPawnsForReading.Contains(pawn);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref slots, "forgeSlots", LookMode.Def);
        Scribe_Collections.Look(ref queued, "forgeQueued", LookMode.Value);
        Scribe_Collections.Look(ref modes, "forgeModes", LookMode.Value);
        Scribe_Collections.Look(ref formatQueue, "forgeFormatQueue", LookMode.Value);
        Scribe_Values.Look(ref formatTicks, "forgeFormatTicks");
        Scribe_Values.Look(ref formatDone, "forgeFormatDone");
        Scribe_Values.Look(ref overdriveStage, "forgeOverdriveStage");
        Scribe_Values.Look(ref overdriveTarget, "forgeOverdriveTarget", -1);
        Scribe_Values.Look(ref wearRemainder, "forgeWearRemainder");
        Scribe_Values.Look(ref robotSlot, "forgeRobotSlot", -1);
        Scribe_Values.Look(ref robotTicks, "forgeRobotTicks");
        Scribe_Collections.Look(ref pendingAnalysis, "forgePending", LookMode.Reference);
        Scribe_Collections.Look(ref slotStuff, "forgeSlotStuff", LookMode.Def);
        slotStuff ??= new List<ThingDef?>();
        slots ??= new List<ThingDef?>();
        queued ??= new List<int>();
        modes ??= new List<int>();
        formatQueue ??= new List<int>();
        pendingAnalysis ??= new List<Thing>();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pendingAnalysis.RemoveAll(t => t == null);
            EnsureSize();
            overdriveStage = Mathf.Clamp(overdriveStage, 0, OverdriveStages - 1);
            overdriveTarget = overdriveTarget < 0 ? overdriveStage : Mathf.Clamp(overdriveTarget, 0, OverdriveStages - 1);
            RefreshOverdriveCache();
        }
    }
}
