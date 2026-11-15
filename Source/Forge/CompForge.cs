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

    // Works its grid on its own, with no pawn and no linked helper. Analysis stays manual.
    public bool automatic;

    // Meditation power taken per draw, instead of or as well as the pipe resource.
    public float meditationPerDraw;

    // Defnames or trade tags this forge refuses to analyze.
    public List<string> exclusionList = new();

    // Percent bonus applied to the analysis success chance (50 => x1.5).
    public float analysisSuccessOffset;
    public int workTicksPerDraw = 10;

    // Percent of max hitpoints lost per second for each overdrive step above 100% (1 => +1% per second per step).
    public float damagePerOverdriveStep = 0.1f;

    // Scales what each overdrive step adds: output, power use and wear per step are all multiplied by this.
    public float stepMultiplier = 1f;

    // Production work speed: run duration is divided by this, for pawns and the Robot Assistant alike.
    public float workSpeedFactor = 1f;

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

public sealed partial class CompForge : ThingComp, IGlowSource
{
    private const int CountRefreshTicks = 60;

    private readonly Dictionary<ThingDef, int> countsOnMap = new();
    private int countsTick = -CountRefreshTicks;

    private List<int> modes = new();
    private List<ThingDef?> slots = new();
    private List<int> queued = new();
    private List<ThingDef?> slotStuff = new();

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

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        MapComponent_WorkBuildings.Of(parent.Map).forges.Add(this);
        RefreshOverdriveCache();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_WorkBuildings.Of(map).forges.Remove(this);
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

    public void FinishProduction(int index)
    {
        EnsureSize();
        ThingDef? def = slots[index];
        // Overdrive wear can destroy the forge on the very tick a run ends.
        if (def == null || !parent.Spawned)
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
        countsOnMap.Clear();
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

    // Refreshed about once a second: "until" slots ask every work tick, and the tab asks for every cell on every frame.
    public int CountOnMap(ThingDef def)
    {
        Map? map = parent.Map;
        if (map == null)
        {
            return 0;
        }
        int now = Find.TickManager.TicksGame;
        if (now - countsTick >= CountRefreshTicks)
        {
            countsOnMap.Clear();
            countsTick = now;
        }
        if (countsOnMap.TryGetValue(def, out int total))
        {
            return total;
        }
        foreach (Thing t in map.listerThings.ThingsOfDef(def))
        {
            total += t.stackCount;
        }
        countsOnMap[def] = total;
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

    public void RemoveQueued(int index, int amount)
    {
        EnsureSize();
        queued[index] = System.Math.Max(0, queued[index] - amount);
        if (queued[index] == 0)
        {
            progress[index] = 0f;
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

    private CompResource? pipeComp;
    private bool pipeCompResolved;

    // Looked up once: the threshold gizmo asks on every frame, and a waiting forge on every tick.
    private CompResource? PipeComp
    {
        get
        {
            if (!pipeCompResolved)
            {
                pipeCompResolved = true;
                pipeComp = Props.pipeNet == null
                    ? null
                    : parent.AllComps.OfType<CompResource>().FirstOrDefault(c => c.Props.pipeNet == Props.pipeNet);
            }
            return pipeComp;
        }
    }

    public bool HasResource
    {
        get
        {
            if (Props.meditationPerDraw > 0f && MapComponent_MeditationPower.Of(parent.Map).Stored < Props.meditationPerDraw)
            {
                return false;
            }
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
        // Both are checked before either is taken, so a forge that needs both never pays one for nothing.
        PipeNet? net = null;
        if (Props.pipeNet != null && Props.pipeAmount > 0f)
        {
            net = PipeComp?.PipeNet;
            if (net == null || net.Stored < Props.pipeAmount)
            {
                return false;
            }
        }
        if (Props.meditationPerDraw > 0f && !MapComponent_MeditationPower.Of(parent.Map).TryDraw(Props.meditationPerDraw))
        {
            return false;
        }
        net?.DrawAmongStorage(Props.pipeAmount, net.storages);
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
        Scribe_Values.Look(ref neutroniumThreshold, "forgeNeutroniumThreshold");
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
