using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

[DefOf]
public static class BOR_SignalsDefOf
{
    public static JobDef BOR_InvestigateSignals = null!;
    public static JobDef BOR_ContactSignals = null!;
    public static JobDef BOR_TalkToShuttlePilot = null!;

    public static ThingDef BOR_DataDrive = null!;
    public static ThingDef BOR_Sunsteel = null!;
    public static ThingDef BOR_SignalShuttle = null!;
    public static ThingDef BOR_SignalShuttleIncoming = null!;
    public static ThingDef BOR_SignalShuttleLeaving = null!;
    public static ThingDef BOR_FilthPodIncoming = null!;

    static BOR_SignalsDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_SignalsDefOf));
    }
}

public enum SignalState
{
    Signals,
    Contact,
    ShuttleLanded
}

// One signal event on an analyzer. An analyzer can carry several; jobs find theirs by index (job.count).
public interface ISignalChannel
{
    bool AwaitingInvestigation { get; }
    float InvestigateHours { get; }
    string InvestigateReport { get; }
    void Notify_Investigated();
    void OpenDialogue(Pawn pawn);
}

public static class SignalUtility
{
    public static bool Powered(Thing t) => t.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;

    public static int AnalysisID(ThingDef def) => def.GetCompProperties<CompProperties_CompAnalyzableUnlockResearch>()?.analysisID ?? -1;

    // Vanilla IsAnalysisComplete reports true when the item has never existed, so only a recorded, satisfied analysis counts.
    public static bool Analyzed(ThingDef def) =>
        Find.AnalysisManager.TryGetAnalysisProgress(AnalysisID(def), out AnalysisDetails details) && details.Satisfied;

    public static ISignalChannel? Channel(Thing? analyzer, int index) =>
        (analyzer as ThingWithComps)?.AllComps.OfType<ISignalChannel>().ElementAtOrDefault(Mathf.Max(0, index));

    public static int IndexOf(ThingComp comp) => comp.parent.AllComps.OfType<ISignalChannel>().ToList().IndexOf((ISignalChannel)comp);

    public static void OpenPawnMenu(ThingComp comp, JobDef jobDef) => OpenPawnMenu(comp.parent, jobDef, IndexOf(comp));

    // Colonist picker that sends the chosen pawn to the target's interaction cell; `index` travels as job.count.
    public static void OpenPawnMenu(Thing analyzer, JobDef jobDef, int index)
    {
        List<FloatMenuOption> options = new();
        foreach (Pawn p in analyzer.Map.mapPawns.FreeColonistsSpawned)
        {
            Pawn pawn = p;
            if (pawn.Downed || !pawn.CanReach(analyzer, PathEndMode.InteractionCell, Danger.Deadly))
            {
                options.Add(new FloatMenuOption(pawn.LabelShortCap + " (" + "BOR_Signals_Incapable".Translate() + ")", null));
                continue;
            }
            options.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
            {
                if (analyzer.Spawned && pawn.Spawned && !pawn.Downed)
                {
                    Job job = JobMaker.MakeJob(jobDef, analyzer);
                    job.count = index;
                    pawn.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
                }
            }));
        }
        if (options.Count == 0)
        {
            Messages.Message("BOR_Signals_NoPawns".Translate(), analyzer, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    public static bool LandingCellOk(IntVec3 c, Map map) =>
        c.InBounds(map) && c.Standable(map) && !c.Roofed(map) && c.GetEdifice(map) == null && c.GetFirstItem(map) == null
        && c.GetFirstPawn(map) == null && !c.Fogged(map);

    public static bool ShuttleFits(IntVec3 c, Map map, IntVec2 size) =>
        GenAdj.OccupiedRect(c, Rot4.North, size).ExpandedBy(1).All(x => LandingCellOk(x, map));

    // Up to `count` landing spots spread over the home area, at least minSpacing apart.
    public static List<IntVec3> RaidLandingSpots(Map map, IntVec2 size, int count, float minSpacing)
    {
        List<IntVec3> spots = new();
        List<IntVec3> home = map.areaManager.Home.ActiveCells.ToList();
        if (home.Count == 0)
        {
            home = map.listerBuildings.allBuildingsColonist.Select(b => b.Position).ToList();
        }
        for (int attempt = 0; attempt < 400 && spots.Count < count && home.Count > 0; attempt++)
        {
            IntVec3 near = home.RandomElement();
            if (!CellFinder.TryFindRandomCellNear(near, map, 12, c => ShuttleFits(c, map, size)
                && spots.All(s => s.DistanceTo(c) >= minSpacing), out IntVec3 cell))
            {
                continue;
            }
            spots.Add(cell);
        }
        return spots;
    }
}
