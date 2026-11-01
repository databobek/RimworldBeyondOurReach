using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class CompProperties_StrangeSignals : CompProperties
{
    public float investigateHours = 4f;
    public int sunsteelPrice = 1000;
    public int shuttleWaitTicks = GenDate.TicksPerDay;

    public CompProperties_StrangeSignals()
    {
        compClass = typeof(CompStrangeSignals);
    }
}

// Solar analyzer side of the data drive event: strange signals -> investigation -> contact dialogue -> shuttle.
public class CompStrangeSignals : ThingComp, ISignalChannel
{
    private SignalState state;
    private Building_SignalShuttle? shuttle;

    public CompProperties_StrangeSignals Props => (CompProperties_StrangeSignals)props;

    public SignalState State => state;

    private bool Powered => SignalUtility.Powered(parent);

    private static int DataDriveAnalysisID => SignalUtility.AnalysisID(BOR_SignalsDefOf.BOR_DataDrive);

    // The event runs until a data drive has been analyzed.
    public static bool DataDriveAnalyzed => SignalUtility.Analyzed(BOR_SignalsDefOf.BOR_DataDrive);

    public bool AwaitingInvestigation => state == SignalState.Signals;

    public float InvestigateHours => Props.investigateHours;

    public string InvestigateReport => "BOR_Signals_InvestigateReport".Translate();

    public void OpenDialogue(Pawn pawn) => OpenSignalDialogue(pawn);

    public void Notify_Investigated()
    {
        state = SignalState.Contact;
    }

    public void Notify_ShuttleLanded(Building_SignalShuttle s)
    {
        shuttle = s;
        state = SignalState.ShuttleLanded;
    }

    public void Notify_ShuttleLeft()
    {
        shuttle = null;
        state = SignalState.Signals;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (!parent.Spawned || parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        if (DataDriveAnalyzed)
        {
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Forget data drive analysis",
                    action = () => Find.AnalysisManager.RemoveAnalysisDetails(DataDriveAnalysisID)
                };
            }
            yield break;
        }

        Command_Action cmd = new Command_Action();
        if (state == SignalState.Signals)
        {
            cmd.defaultLabel = "";
            cmd.defaultDesc = "BOR_Signals_Strange".Translate();
            cmd.icon = BaseContent.ClearTex;
            cmd.action = () => SignalUtility.OpenPawnMenu(this, BOR_SignalsDefOf.BOR_InvestigateSignals);
        }
        else
        {
            cmd.defaultLabel = "BOR_Signals_Contact_Label".Translate();
            cmd.defaultDesc = "BOR_Signals_Contact_Desc".Translate();
            cmd.icon = BOR_SignalsDefOf.BOR_DataDrive.uiIcon;
            cmd.action = () => SignalUtility.OpenPawnMenu(this, BOR_SignalsDefOf.BOR_ContactSignals);
        }
        if (!Powered)
        {
            cmd.Disable("NoPower".Translate());
        }
        else if (state == SignalState.ShuttleLanded)
        {
            cmd.Disable("BOR_Signals_ShuttleWaiting".Translate());
        }
        else if (parent.Map.GetComponent<MapComponent_SignalConsequences>().Pending)
        {
            cmd.Disable("BOR_Signals_Silent".Translate());
        }
        yield return cmd;

        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }
        if (state == SignalState.Signals)
        {
            yield return new Command_Action { defaultLabel = "DEV: Finish investigation", action = Notify_Investigated };
        }
        MapComponent_SignalConsequences consequences = parent.Map.GetComponent<MapComponent_SignalConsequences>();
        yield return new Command_Action { defaultLabel = "DEV: Consequence now", action = () => consequences.Fire(-1) };
        yield return new Command_Action { defaultLabel = "DEV: Laser", action = () => consequences.Fire(0) };
        yield return new Command_Action { defaultLabel = "DEV: Junk pods", action = () => consequences.Fire(1) };
        yield return new Command_Action { defaultLabel = "DEV: Wastepacks / drone", action = () => consequences.Fire(2) };
        if (consequences.Pending)
        {
            yield return new Command_Action { defaultLabel = "DEV: Clear pending consequence", action = consequences.ClearPending };
        }
    }

    public void OpenSignalDialogue(Pawn pawn)
    {
        if (state != SignalState.Contact)
        {
            return;
        }
        DiaNode greeting = new DiaNode("BOR_Signals_Greeting".Translate(pawn.Named("PAWN")));
        DiaNode come = new DiaNode("BOR_Signals_WantMeToCome".Translate(pawn.Named("PAWN")));
        DiaNode pity = new DiaNode("BOR_Signals_Pity".Translate());
        pity.options.Add(new DiaOption("BOR_Signals_Close".Translate()) { resolveTree = true });
        greeting.options.Add(new DiaOption("BOR_Signals_Confirm".Translate()) { link = come });
        greeting.options.Add(new DiaOption("BOR_Signals_EndConversation".Translate()) { link = pity });
        come.options.Add(new DiaOption("BOR_Signals_Confirm".Translate()) { action = CallShuttle, resolveTree = true });
        come.options.Add(new DiaOption("BOR_Signals_EndConversation".Translate()) { link = pity });
        Find.WindowStack.Add(new Dialog_NodeTree(greeting, true, false, "BOR_Signals_Title".Translate()));
    }

    private void CallShuttle()
    {
        if (!parent.Spawned || state != SignalState.Contact)
        {
            return;
        }
        Map map = parent.Map;
        if (!TryFindLandingSpot(map, out IntVec3 cell))
        {
            Messages.Message("BOR_Signals_NoLandingSpot".Translate(), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Building_SignalShuttle s = (Building_SignalShuttle)ThingMaker.MakeThing(BOR_SignalsDefOf.BOR_SignalShuttle);
        s.Init(this, Props.shuttleWaitTicks, Props.sunsteelPrice);
        SkyfallerMaker.SpawnSkyfaller(BOR_SignalsDefOf.BOR_SignalShuttleIncoming, s, cell, map);
        Notify_ShuttleLanded(s);
        Find.LetterStack.ReceiveLetter("BOR_Signals_ArrivalLabel".Translate(), "BOR_Signals_ArrivalText".Translate(),
            LetterDefOf.NeutralEvent, new TargetInfo(cell, map));
    }

    // Closest open, roofless spot near the analyzer the whole shuttle fits on.
    private bool TryFindLandingSpot(Map map, out IntVec3 result)
    {
        IntVec2 size = BOR_SignalsDefOf.BOR_SignalShuttle.size;
        foreach (IntVec3 c in GenRadial.RadialCellsAround(parent.Position, 30f, false))
        {
            if (!c.InBounds(map) || !map.reachability.CanReach(parent.Position, c, PathEndMode.Touch, TraverseParms.For(TraverseMode.PassDoors)))
            {
                continue;
            }
            if (SignalUtility.ShuttleFits(c, map, size))
            {
                result = c;
                return true;
            }
        }
        result = IntVec3.Invalid;
        return false;
    }

    public override string CompInspectStringExtra()
    {
        if (DataDriveAnalyzed || state == SignalState.Signals)
        {
            return null!;
        }
        return state == SignalState.Contact ? "BOR_Signals_InspectContact".Translate() : "BOR_Signals_ShuttleWaiting".Translate();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref state, "signalState");
        Scribe_References.Look(ref shuttle, "signalShuttle");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && state == SignalState.ShuttleLanded && (shuttle == null || shuttle.Destroyed))
        {
            state = SignalState.Signals;
        }
    }
}

public class JobDriver_InvestigateSignals : JobDriver
{
    private const TargetIndex AnalyzerInd = TargetIndex.A;

    private int totalTicks;
    private int ticksDone;

    private Thing Analyzer => job.GetTarget(AnalyzerInd).Thing;

    private ISignalChannel? Channel => SignalUtility.Channel(Analyzer, job.count);

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Analyzer, job, 1, -1, null, errorOnFailed);

    public override string GetReport() => Channel?.InvestigateReport ?? base.GetReport();

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(AnalyzerInd);
        this.FailOn(() => !SignalUtility.Powered(Analyzer));
        this.FailOn(() => Channel == null || !Channel.AwaitingInvestigation);

        yield return Toils_Goto.GotoThing(AnalyzerInd, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("InvestigateSignals");
        work.initAction = () =>
        {
            float hours = Channel!.InvestigateHours;
            totalTicks = Mathf.CeilToInt(hours * GenDate.TicksPerHour / Mathf.Max(0.1f, pawn.GetStatValue(StatDefOf.ResearchSpeed)));
            ticksDone = 0;
        };
        work.tickAction = () =>
        {
            ticksDone++;
            pawn.rotationTracker.FaceTarget(Analyzer);
            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.05f);
            if (ticksDone >= totalTicks)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithProgressBar(AnalyzerInd, () => totalTicks <= 0 ? 0f : (float)ticksDone / totalTicks);
        work.activeSkill = () => SkillDefOf.Intellectual;
        yield return work;

        yield return Toils_General.Do(() => Channel?.Notify_Investigated());
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref totalTicks, "totalTicks");
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}

public class JobDriver_ContactSignals : JobDriver
{
    private Thing Analyzer => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Analyzer, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        yield return Toils_General.Do(() => SignalUtility.Channel(Analyzer, job.count)?.OpenDialogue(pawn));
    }
}

public class JobDriver_TalkToShuttlePilot : JobDriver
{
    private Building_SignalShuttle? Shuttle => job.GetTarget(TargetIndex.A).Thing as Building_SignalShuttle;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Do(() => Shuttle?.OpenPilotDialogue(pawn));
    }
}
