using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Blue Kwazaari shuttle. Delivery: lands, lets the lent soldiers out one by one, leaves.
// Pickup: lands when the loan ends, orders the lent soldiers aboard, leaves when all boarded or the wait runs out.
public class Building_KwazaariShuttle : Building
{
    private const int ReleaseInterval = 30;
    private const int OrderInterval = 250;
    private const int PickupWaitTicks = 2 * GenDate.TicksPerHour;

    private bool pickup;
    private List<Pawn> passengers = new();
    private int ticksLeft;
    private bool ordersGiven;
    private bool departed;

    private static KwazaariLoan? LoanFor(Map map) => GameComponent_Kwazaari.Instance.Loans.FirstOrDefault(l => l.map == map);

    public static bool TryFindLandingSpot(Map map, out IntVec3 result)
    {
        IntVec2 size = BOR_KwazaariDefOf.BOR_KwazaariShuttle.size;
        IntVec3 center = DropCellFinder.TradeDropSpot(map);
        foreach (IntVec3 c in GenRadial.RadialCellsAround(center, 30f, true))
        {
            if (SignalUtility.ShuttleFits(c, map, size))
            {
                result = c;
                return true;
            }
        }
        return SignalUtility.RaidLandingSpots(map, size, 1, 0f).TryRandomElement(out result);
    }

    public static void SendDelivery(KwazaariLoan loan, IntVec3 cell)
    {
        Map map = loan.map!;
        Building_KwazaariShuttle shuttle = (Building_KwazaariShuttle)ThingMaker.MakeThing(BOR_KwazaariDefOf.BOR_KwazaariShuttle);
        shuttle.passengers = loan.pawns.ToList();
        SkyfallerMaker.SpawnSkyfaller(BOR_KwazaariDefOf.BOR_KwazaariShuttleIncoming, shuttle, cell, map);
        Find.LetterStack.ReceiveLetter("BOR_Kwazaari_LoanArrivalLabel".Translate(),
            "BOR_Kwazaari_LoanArrivalText".Translate(loan.pawns.Count, ((loan.returnTick - Find.TickManager.TicksGame)).ToStringTicksToPeriod()),
            LetterDefOf.PositiveEvent, new TargetInfo(cell, map));
    }

    public static bool TrySendPickup(KwazaariLoan loan)
    {
        Map map = loan.map!;
        if (!TryFindLandingSpot(map, out IntVec3 cell))
        {
            return false;
        }
        Building_KwazaariShuttle shuttle = (Building_KwazaariShuttle)ThingMaker.MakeThing(BOR_KwazaariDefOf.BOR_KwazaariShuttle);
        shuttle.pickup = true;
        shuttle.ticksLeft = PickupWaitTicks;
        SkyfallerMaker.SpawnSkyfaller(BOR_KwazaariDefOf.BOR_KwazaariShuttleIncoming, shuttle, cell, map);
        Find.LetterStack.ReceiveLetter("BOR_Kwazaari_PickupLabel".Translate(),
            "BOR_Kwazaari_PickupText".Translate(PickupWaitTicks.ToStringTicksToPeriod(), loan.lostPawnCost.ToString("0.#")),
            LetterDefOf.NeutralEvent, new TargetInfo(cell, map));
        return true;
    }

    public override AcceptanceReport DeconstructibleBy(Faction faction) => false;

    public override AcceptanceReport ClaimableBy(Faction by) => false;

    protected override void Tick()
    {
        base.Tick();
        if (departed || !Spawned)
        {
            return;
        }
        if (!pickup)
        {
            if (this.IsHashIntervalTick(ReleaseInterval))
            {
                ReleaseNext();
            }
            return;
        }
        ticksLeft--;
        KwazaariLoan? loan = LoanFor(Map);
        if (loan == null)
        {
            Depart();
            return;
        }
        if (!ordersGiven || this.IsHashIntervalTick(OrderInterval))
        {
            OrderBoarding(loan, !ordersGiven);
            ordersGiven = true;
        }
        if (ticksLeft <= 0 || !loan.pawns.Any(p => CanStillBoard(p)))
        {
            Depart();
        }
    }

    private void ReleaseNext()
    {
        if (passengers.Count == 0)
        {
            Depart();
            return;
        }
        Pawn pawn = passengers[0];
        passengers.RemoveAt(0);
        if (!CellFinder.TryFindRandomCellNear(Position, Map, 6, c => c.Standable(Map) && !this.OccupiedRect().Contains(c), out IntVec3 cell))
        {
            cell = InteractionCell;
        }
        GenSpawn.Spawn(pawn, cell, Map);
        pawn.SetFaction(Faction.OfPlayer);
        pawn.apparel?.LockAll();
        pawn.health.AddHediff(BOR_KwazaariDefOf.BOR_KwazaariLoan);
    }

    private bool CanStillBoard(Pawn? p) => p != null && p.Spawned && p.Map == Map && !p.Dead;

    private void OrderBoarding(KwazaariLoan loan, bool undraft)
    {
        foreach (Pawn p in loan.pawns)
        {
            if (!CanStillBoard(p) || p.Downed || p.InMentalState)
            {
                continue;
            }
            if (undraft && p.drafter != null)
            {
                p.drafter.Drafted = false;
            }
            if (p.Drafted || p.CurJobDef == BOR_KwazaariDefOf.BOR_BoardKwazaariShuttle
                || !p.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                continue;
            }
            p.jobs?.TryTakeOrderedJob(JobMaker.MakeJob(BOR_KwazaariDefOf.BOR_BoardKwazaariShuttle, this), JobTag.Misc);
        }
    }

    // The pawn returns to the Kwazaari, taking whatever it carries and wears.
    public void Board(Pawn pawn)
    {
        if (departed || !pickup || !GameComponent_Kwazaari.Instance.IsBorrowed(pawn))
        {
            return;
        }
        GameComponent_Kwazaari.Instance.Notify_Returned(pawn);
        pawn.DeSpawn();
        pawn.SetFaction(GameComponent_Kwazaari.Faction);
        // Not discarded here: we are inside the pawn's own job. World pawn GC drops it later.
        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
    }

    private void Depart()
    {
        if (departed || !Spawned)
        {
            return;
        }
        departed = true;
        Map map = Map;
        IntVec3 pos = Position;
        if (pickup)
        {
            KwazaariLoan? loan = LoanFor(map);
            if (loan != null)
            {
                GameComponent_Kwazaari.Instance.Notify_PickupDeparted(loan);
            }
        }
        Destroy(DestroyMode.Vanish);
        SkyfallerMaker.SpawnSkyfaller(BOR_KwazaariDefOf.BOR_KwazaariShuttleLeaving, pos, map);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo g in base.GetGizmos())
        {
            yield return g;
        }
        if (DebugSettings.ShowDevGizmos && pickup && !departed)
        {
            yield return new Command_Action { defaultLabel = "DEV: Leave now", action = () => ticksLeft = 0 };
        }
    }

    public override string GetInspectString()
    {
        string s = base.GetInspectString();
        if (pickup && !departed)
        {
            s += (s.NullOrEmpty() ? "" : "\n") + "BOR_Kwazaari_PickupLeavesIn".Translate(ticksLeft.ToStringTicksToPeriod());
        }
        return s;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref pickup, "pickup");
        Scribe_Collections.Look(ref passengers, "passengers", LookMode.Deep);
        Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        Scribe_Values.Look(ref ordersGiven, "ordersGiven");
        Scribe_Values.Look(ref departed, "departed");
        passengers ??= new List<Pawn>();
    }
}

public class JobDriver_BoardKwazaariShuttle : JobDriver
{
    private Building_KwazaariShuttle? Shuttle => job.GetTarget(TargetIndex.A).Thing as Building_KwazaariShuttle;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Do(() => Shuttle?.Board(pawn));
    }
}
