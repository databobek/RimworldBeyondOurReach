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

// The trader's shuttle: sells one data drive for sunsteel, and leaves angry if refused or ignored.
public class Building_SignalShuttle : Building
{
    private Thing? analyzer;
    private int ticksLeft;
    private int price;
    private bool departed;
    private bool farewellOpen;

    public void Init(CompStrangeSignals comp, int waitTicks, int sunsteelPrice)
    {
        analyzer = comp.parent;
        ticksLeft = waitTicks;
        price = sunsteelPrice;
    }

    public override AcceptanceReport DeconstructibleBy(Faction faction) => false;

    public override AcceptanceReport ClaimableBy(Faction by) => false;

    protected override void Tick()
    {
        base.Tick();
        if (departed || farewellOpen)
        {
            return;
        }
        ticksLeft--;
        if (ticksLeft <= 0)
        {
            OpenFarewell();
        }
    }

    private static int SunsteelOnMap(Map map) => map.listerThings.ThingsOfDef(BOR_SignalsDefOf.BOR_Sunsteel).Sum(t => t.stackCount);

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo g in base.GetGizmos())
        {
            yield return g;
        }
        if (departed)
        {
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "BOR_Signals_TalkToPilot".Translate(),
            defaultDesc = "BOR_Signals_TalkToPilotDesc".Translate(price, ticksLeft.ToStringTicksToPeriod()),
            icon = BOR_SignalsDefOf.BOR_DataDrive.uiIcon,
            action = OpenPawnMenu
        };
        if (DebugSettings.ShowDevGizmos && !farewellOpen)
        {
            yield return new Command_Action { defaultLabel = "DEV: Time out now", action = () => ticksLeft = 1 };
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption o in base.GetFloatMenuOptions(selPawn))
        {
            yield return o;
        }
        if (departed || !selPawn.IsColonistPlayerControlled)
        {
            yield break;
        }
        if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
        {
            yield return new FloatMenuOption("BOR_Signals_TalkToPilot".Translate() + " (" + "NoPath".Translate() + ")", null);
            yield break;
        }
        yield return new FloatMenuOption("BOR_Signals_TalkToPilot".Translate(), () => OrderTalk(selPawn));
    }

    private void OpenPawnMenu()
    {
        List<FloatMenuOption> options = new();
        foreach (Pawn p in Map.mapPawns.FreeColonistsSpawned)
        {
            Pawn pawn = p;
            if (pawn.Downed || !pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                options.Add(new FloatMenuOption(pawn.LabelShortCap + " (" + "BOR_Signals_Incapable".Translate() + ")", null));
                continue;
            }
            options.Add(new FloatMenuOption(pawn.LabelShortCap, () => OrderTalk(pawn)));
        }
        if (options.Count == 0)
        {
            Messages.Message("BOR_Signals_NoPawns".Translate(), this, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OrderTalk(Pawn pawn)
    {
        if (!departed && Spawned && pawn.Spawned && !pawn.Downed)
        {
            pawn.jobs?.TryTakeOrderedJob(JobMaker.MakeJob(BOR_SignalsDefOf.BOR_TalkToShuttlePilot, this), JobTag.Misc);
        }
    }

    public void OpenPilotDialogue(Pawn pawn)
    {
        if (departed || farewellOpen || !Spawned)
        {
            return;
        }
        DiaNode offer = new DiaNode("BOR_Signals_PilotOffer".Translate(price, pawn.Named("PAWN")));
        DiaNode farewell = FarewellNode();
        DiaNode thanks = new DiaNode("BOR_Signals_Thanks".Translate());
        thanks.options.Add(new DiaOption("BOR_Signals_Close".Translate()) { resolveTree = true });
        DiaOption yes = new DiaOption("BOR_Signals_Yes".Translate()) { action = () => Buy(pawn), link = thanks };
        if (SunsteelOnMap(Map) < price)
        {
            yes.Disable("BOR_Signals_NotEnoughSunsteel".Translate(price));
        }
        offer.options.Add(yes);
        offer.options.Add(new DiaOption("BOR_Signals_No".Translate()) { action = RefuseOffer, link = farewell });
        Find.WindowStack.Add(new Dialog_NodeTree(offer, true, false, "BOR_Signals_PilotTitle".Translate()));
    }

    private void RefuseOffer()
    {
        farewellOpen = true;
        ticksLeft = 0;
    }

    private void OpenFarewell()
    {
        farewellOpen = true;
        Find.WindowStack.Add(new Dialog_NodeTree(FarewellNode(), true, false, "BOR_Signals_PilotTitle".Translate()));
    }

    private DiaNode FarewellNode()
    {
        DiaNode farewell = new DiaNode("BOR_Signals_Farewell".Translate());
        farewell.options.Add(new DiaOption("BOR_Signals_Close".Translate()) { action = () => Depart(true), resolveTree = true });
        return farewell;
    }

    private void Buy(Pawn pawn)
    {
        if (departed || !Spawned || SunsteelOnMap(Map) < price)
        {
            return;
        }
        int remaining = price;
        foreach (Thing t in Map.listerThings.ThingsOfDef(BOR_SignalsDefOf.BOR_Sunsteel).ToList())
        {
            int take = Mathf.Min(remaining, t.stackCount);
            t.SplitOff(take).Destroy();
            remaining -= take;
            if (remaining <= 0)
            {
                break;
            }
        }
        IntVec3 dropAt = pawn.Spawned && pawn.Map == Map ? pawn.Position : InteractionCell;
        GenPlace.TryPlaceThing(ThingMaker.MakeThing(BOR_SignalsDefOf.BOR_DataDrive), dropAt, Map, ThingPlaceMode.Near);
        Depart(false);
    }

    private void Depart(bool refused)
    {
        if (departed || !Spawned)
        {
            return;
        }
        departed = true;
        Map map = Map;
        IntVec3 pos = Position;
        (analyzer?.TryGetComp<CompStrangeSignals>())?.Notify_ShuttleLeft();
        if (refused)
        {
            map.GetComponent<MapComponent_SignalConsequences>().Schedule();
        }
        Destroy(DestroyMode.Vanish);
        SkyfallerMaker.SpawnSkyfaller(BOR_SignalsDefOf.BOR_SignalShuttleLeaving, pos, map);
    }

    public override string GetInspectString()
    {
        string s = base.GetInspectString();
        if (!departed)
        {
            s += (s.NullOrEmpty() ? "" : "\n") + "BOR_Signals_ShuttleLeavesIn".Translate(ticksLeft.ToStringTicksToPeriod());
        }
        return s;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref analyzer, "analyzer");
        Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        Scribe_Values.Look(ref price, "price");
        Scribe_Values.Look(ref departed, "departed");
        // Open windows are not saved: farewellOpen stays false after loading, and ticksLeft <= 0 reopens the farewell.
    }
}

// Drop pod that bursts into vomit, excrement and garbage where it lands.
public class Skyfaller_FilthPod : Skyfaller
{
    private static readonly string[] FilthDefs = { "Filth_Vomit", "Filth_AnimalFilth", "Filth_Trash", "Filth_Slime", "Filth_CorpseBile" };

    protected override void Impact()
    {
        if (Map != null)
        {
            int filth = Rand.RangeInclusive(50, 70);
            for (int i = 0; i < filth; i++)
            {
                IntVec3 c = Position + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(5.9f))];
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(FilthDefs.RandomElement());
                if (c.InBounds(Map) && def != null)
                {
                    FilthMaker.TryMakeFilth(c, Map, def, 3);
                }
            }
        }
        base.Impact();
    }
}

// Punishments scheduled when the shuttle pilot is refused or ignored.
public class MapComponent_SignalConsequences : MapComponent
{
    private const int CheckInterval = 250;

    private List<int> pendingTicks = new();

    public MapComponent_SignalConsequences(Map map) : base(map)
    {
    }

    public bool Pending => pendingTicks.Count > 0;

    public void Schedule()
    {
        pendingTicks.Add(Find.TickManager.TicksGame + Rand.RangeInclusive(2 * GenDate.TicksPerHour, 6 * GenDate.TicksPerHour));
    }

    public override void MapComponentTick()
    {
        if (pendingTicks.Count == 0 || Find.TickManager.TicksGame % CheckInterval != 0)
        {
            return;
        }
        int now = Find.TickManager.TicksGame;
        for (int i = pendingTicks.Count - 1; i >= 0; i--)
        {
            if (pendingTicks[i] <= now)
            {
                pendingTicks.RemoveAt(i);
                Fire(-1);
            }
        }
    }

    public void ClearPending() => pendingTicks.Clear();

    // forced: -1 rolls 20/50/30; 0 laser, 1 junk pods, 2 wastepacks (or psychic drone without Biotech).
    public void Fire(int forced)
    {
        float roll = forced switch
        {
            0 => 0f,
            1 => 0.5f,
            2 => 0.9f,
            _ => Rand.Value
        };
        if (roll < 0.2f)
        {
            OrbitalLaser();
        }
        else if (roll < 0.7f)
        {
            FilthPods();
        }
        else if (ModsConfig.BiotechActive)
        {
            Wastepacks();
        }
        else
        {
            PsychicDrone();
        }
    }

    private IntVec3 RandomHomeCell()
    {
        List<IntVec3> home = map.areaManager.Home.ActiveCells.Where(c => !c.Fogged(map)).ToList();
        if (home.Count > 0)
        {
            return home.RandomElement();
        }
        List<Building> buildings = map.listerBuildings.allBuildingsColonist;
        return buildings.Count > 0 ? buildings.RandomElement().Position : CellFinder.RandomNotEdgeCell(10, map);
    }

    private IntVec3 DropSpotNear(IntVec3 c) =>
        DropCellFinder.TryFindDropSpotNear(c, map, out IntVec3 spot, false, true) ? spot : c;

    private void OrbitalLaser()
    {
        IntVec3 cell = RandomHomeCell();
        PowerBeam beam = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, cell, map);
        // A short zap, not the 10-second wandering beam of the vanilla permit.
        beam.duration = 45;
        beam.StartStrike();
        CameraJumper.TryJump(cell, map);
    }

    private void FilthPods()
    {
        IntVec3 first = IntVec3.Invalid;
        int pods = Rand.RangeInclusive(15, 25);
        for (int i = 0; i < pods; i++)
        {
            IntVec3 cell = DropSpotNear(RandomHomeCell());
            SkyfallerMaker.SpawnSkyfaller(BOR_SignalsDefOf.BOR_FilthPodIncoming, ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), cell, map);
            if (!first.IsValid)
            {
                first = cell;
            }
        }
        CameraJumper.TryJump(first, map);
    }

    private void Wastepacks()
    {
        ThingDef wastepack = DefDatabase<ThingDef>.GetNamed("Wastepack");
        const int total = 1000;
        const int batches = 20;
        IntVec3 first = IntVec3.Invalid;
        for (int b = 0; b < batches; b++)
        {
            List<Thing> things = new();
            for (int left = total / batches; left > 0; left -= wastepack.stackLimit)
            {
                Thing t = ThingMaker.MakeThing(wastepack);
                t.stackCount = Mathf.Min(left, wastepack.stackLimit);
                things.Add(t);
            }
            IntVec3 cell = DropSpotNear(RandomHomeCell());
            DropPodUtility.DropThingsNear(cell, map, things);
            if (!first.IsValid)
            {
                first = cell;
            }
        }
        CameraJumper.TryJump(first, map);
    }

    private void PsychicDrone()
    {
        GameCondition_PsychicEmanation drone = (GameCondition_PsychicEmanation)GameConditionMaker.MakeCondition(
            GameConditionDefOf.PsychicDrone, Rand.RangeInclusive(GenDate.TicksPerHour * 12, GenDate.TicksPerDay * 3));
        drone.gender = Rand.Bool ? Gender.Male : Gender.Female;
        drone.level = PsychicDroneLevel.BadHigh;
        map.gameConditionManager.RegisterCondition(drone);
        CameraJumper.TryJump(map.Center, map);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref pendingTicks, "pendingTicks", LookMode.Value);
        pendingTicks ??= new List<int>();
    }
}
