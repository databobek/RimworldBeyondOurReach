using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using RimWorld;
using Verse;

namespace BeyondOurReach;

public class CompProperties_Forge : CompProperties
{
    public int gridWidth = 2;
    public int gridHeight = 4;

    public PipeNetDef? pipeNet;
    public float pipeAmount;
    public int workTicksPerDraw = 10;

    public CompProperties_Forge()
    {
        compClass = typeof(CompForge);
    }

    public int Capacity => gridWidth * gridHeight;
}

public class CompForge : ThingComp
{
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
        queued[index] = System.Math.Max(0, queued[index] - 1);
    }

    public int NextProductionSlot()
    {
        EnsureSize();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && queued[i] > 0)
            {
                return i;
            }
        }
        return -1;
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
        Scribe_Collections.Look(ref pendingAnalysis, "forgePending", LookMode.Reference);
        Scribe_Collections.Look(ref slotStuff, "forgeSlotStuff", LookMode.Def);
        slotStuff ??= new List<ThingDef?>();
        slots ??= new List<ThingDef?>();
        queued ??= new List<int>();
        pendingAnalysis ??= new List<Thing>();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pendingAnalysis.RemoveAll(t => t == null);
            EnsureSize();
        }
    }
}
