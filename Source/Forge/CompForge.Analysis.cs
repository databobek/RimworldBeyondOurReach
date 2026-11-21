using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BeyondOurReach;

public partial class CompForge
{
    private List<Thing> pendingAnalysis = new();

    public Thing? AnalyzingThing;
    public float AnalysisProgress;

    public IReadOnlyList<Thing> PendingAnalysis
    {
        get
        {
            pendingAnalysis.RemoveAll(t => t == null || t.Destroyed);
            return pendingAnalysis;
        }
    }

    public int FreeSlotCount => slots.Count(s => s == null) - PendingAnalysis.Count;

    public bool IsExcluded(ThingDef def)
    {
        List<string> list = Props.exclusionList;
        if (def == null || list == null || list.Count == 0)
        {
            return false;
        }
        // The mod settings can make the techprof subpersona core replicable.
        if (def.defName == "TechprofSubpersonaCore" && BorSettings.ReplicateTechprof)
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

    private const int StuckAnalysisCheckInterval = 250;
    private const int StuckAnalysisTicks = GenDate.TicksPerHour;

    // How long each queued item has been out of every colonist's reach. Not saved: a load just restarts the wait.
    private readonly Dictionary<Thing, int> analysisStuckTicks = new();

    // An item no colonist can bring is on the map, allowed, and reachable by none of the pawns who may work this forge.
    private bool CanBeBrought(Thing item)
    {
        // In a pawn's hands or a container for the moment; not lost.
        if (!item.Spawned)
        {
            return item.MapHeld == parent.Map;
        }
        if (item.Map != parent.Map || item.IsForbidden(Faction.OfPlayer))
        {
            return false;
        }
        foreach (Pawn pawn in parent.Map.mapPawns.FreeColonistsSpawned)
        {
            if (!pawn.Downed && IsAssignedOrUnassigned(pawn) && pawn.CanReach(item, Verse.AI.PathEndMode.ClosestTouch, Danger.Deadly)
                && pawn.CanReach(parent, Verse.AI.PathEndMode.InteractionCell, Danger.Deadly))
            {
                return true;
            }
        }
        return false;
    }

    // Queued analysis holds up a forge that works on its own, so an item nobody can bring for an hour is dropped from
    // the queue, with a letter. Called on StuckAnalysisCheckInterval.
    private void DropStuckAnalysis()
    {
        foreach (Thing item in PendingAnalysis.ToList())
        {
            if (CanBeBrought(item))
            {
                analysisStuckTicks.Remove(item);
                continue;
            }
            analysisStuckTicks.TryGetValue(item, out int stuck);
            stuck += StuckAnalysisCheckInterval;
            if (stuck < StuckAnalysisTicks)
            {
                analysisStuckTicks[item] = stuck;
                continue;
            }
            analysisStuckTicks.Remove(item);
            RemovePendingAnalysis(item);
            Find.LetterStack.ReceiveLetter("BOR_Forge_AnalysisDroppedLabel".Translate(),
                "BOR_Forge_AnalysisDroppedText".Translate(item.LabelNoCount, parent.LabelCap), LetterDefOf.NeutralEvent, new LookTargets(parent, item));
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
}
