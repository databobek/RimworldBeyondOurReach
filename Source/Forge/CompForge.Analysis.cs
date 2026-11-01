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
