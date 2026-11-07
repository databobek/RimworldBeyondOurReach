using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// The Dark Archotech's answer to the Path of Light: a bolt, an Umbral, one dead colonist, and it is gone.
public static class UmbralHerald
{
    private const int MaxCellTries = 50;
    private const int FallbackRadius = 15;

    public static bool IsHerald(Pawn pawn) => pawn.health.hediffSet.HasHediff(BOR_UmbralDefOf.BOR_UmbralHerald);

    // The colony's own Umbrals are about to die in the judgment, so they make poor targets.
    public static Pawn? ClosestColonist(IntVec3 cell, Map map) =>
        map.mapPawns.FreeColonistsSpawned
            .Where(p => !Umbral.IsUmbral(p) && map.reachability.CanReach(cell, p, PathEndMode.Touch, TraverseMode.PassDoors, Danger.Deadly))
            .OrderBy(p => p.Position.DistanceToSquared(cell))
            .FirstOrDefault();

    public static void Strike(Pawn? chooser)
    {
        Map? map = chooser?.MapHeld is { IsPlayerHome: true } home
            ? home
            : Find.Maps.FirstOrDefault(m => m.IsPlayerHome && m.mapPawns.FreeColonistsSpawned.Any());
        if (map == null || !TryFindStrike(map, out IntVec3 cell, out Pawn target))
        {
            Log.Warning("[BOR] The Umbral herald found nowhere to strike: no home map with a reachable non-Umbral colonist.");
            return;
        }
        // There is no letter at this point, so the player is taken to it.
        CameraJumper.TryJump(cell, map);
        map.weatherManager.eventHandler.AddEvent(new WeatherEvent_RedLightning(map, cell));
        Faction? faction = Find.FactionManager.FirstFactionOfDef(BOR_UmbralDefOf.BOR_Umbrals);
        Pawn herald = PawnGenerator.GeneratePawn(BOR_UmbralDefOf.BOR_Umbral_Kind, faction);
        GenSpawn.Spawn(herald, cell, map);
        Hediff_UmbralHerald hediff = (Hediff_UmbralHerald)HediffMaker.MakeHediff(BOR_UmbralDefOf.BOR_UmbralHerald, herald);
        hediff.target = target;
        herald.health.AddHediff(hediff);
    }

    private static bool TryFindStrike(Map map, out IntVec3 cell, out Pawn target)
    {
        // The home area only grows around buildings, so a young colony may not have one; then near a colonist will do.
        IEnumerable<IntVec3> homeCells = map.areaManager.Home.ActiveCells.Where(c => c.Standable(map) && !c.Fogged(map)).InRandomOrder().Take(MaxCellTries);
        IEnumerable<IntVec3> nearColonists = map.mapPawns.FreeColonistsSpawned.Where(p => !Umbral.IsUmbral(p)).InRandomOrder()
            .Select(p => CellFinder.RandomClosewalkCellNear(p.Position, map, FallbackRadius));
        foreach (IntVec3 c in homeCells.Concat(nearColonists))
        {
            if (ClosestColonist(c, map) is Pawn found)
            {
                cell = c;
                target = found;
                return true;
            }
        }
        cell = IntVec3.Invalid;
        target = null!;
        return false;
    }
}

// Has to be read: it never times out and a right click does not dismiss it. Closing it sets off the herald.
public class ChoiceLetter_DarkDispleasure : ChoiceLetter
{
    public override bool CanDismissWithRightClick => false;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            yield return new DiaOption("OK".Translate())
            {
                action = () =>
                {
                    Find.LetterStack.RemoveLetter(this);
                    GameComponent_OrientisPath.Instance.ArmHerald();
                },
                resolveTree = true
            };
        }
    }
}

// Hidden. Keeps the Umbral on the job whatever else tries to give it one.
public class Hediff_UmbralHerald : Hediff
{
    private const int CheckInterval = 30;

    public Pawn? target;

    public override bool Visible => false;

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!pawn.IsHashIntervalTick(CheckInterval, delta) || !pawn.Spawned || pawn.Downed || pawn.CurJobDef == BOR_UmbralDefOf.BOR_UmbralMurder)
        {
            return;
        }
        // Carried off or left the map: the next closest will do. If they died some other way, the work is done.
        if (target != null && !target.Dead && !target.Spawned)
        {
            target = UmbralHerald.ClosestColonist(pawn.Position, pawn.Map);
        }
        Job job = JobMaker.MakeJob(BOR_UmbralDefOf.BOR_UmbralMurder, target != null && !target.Dead ? target : LocalTargetInfo.Invalid);
        job.locomotionUrgency = LocomotionUrgency.Sprint;
        job.canBashDoors = true;
        job.canBashFences = true;
        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref target, "target");
    }
}

// With no target left, only the skip away remains.
public class JobDriver_UmbralMurder : JobDriver
{
    private const int TearIntervalTicks = 30;
    private const int WorkTicks = 180;

    private int ticksDone;

    private Pawn? Victim => job.targetA.Thing as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        if (Victim != null)
        {
            this.AddFinishAction(cond =>
            {
                if (Victim != null && !Victim.Dead)
                {
                    Umbral.RemoveHediffs(Victim, BOR_UmbralDefOf.BOR_SlaughterRestraint);
                }
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);

            Toil pin = ToilMaker.MakeToil("UmbralMurderPin");
            pin.initAction = () => Victim?.health.AddHediff(BOR_UmbralDefOf.BOR_SlaughterRestraint);
            pin.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pin;

            Toil tear = ToilMaker.MakeToil("UmbralMurderTear");
            tear.initAction = () => ticksDone = 0;
            tear.tickAction = () =>
            {
                ticksDone++;
                if (ticksDone % TearIntervalTicks == 0 && Victim != null)
                {
                    RighteousSlaughter.TearLimb(Victim);
                }
                if (ticksDone >= WorkTicks)
                {
                    ReadyForNextToil();
                }
            };
            tear.defaultCompleteMode = ToilCompleteMode.Never;
            tear.FailOnDespawnedOrNull(TargetIndex.A);
            tear.WithProgressBar(TargetIndex.A, () => (float)ticksDone / WorkTicks);
            yield return tear;

            Toil kill = ToilMaker.MakeToil("UmbralMurderKill");
            kill.initAction = () =>
            {
                if (Victim != null && !Victim.Dead)
                {
                    ExecutionUtility.DoExecutionByCut(pawn, Victim);
                }
            };
            kill.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return kill;
        }

        Toil skip = ToilMaker.MakeToil("UmbralMurderSkip");
        skip.initAction = () =>
        {
            EffecterDefOf.Skip_EntryNoDelay.Spawn(pawn, pawn.Map).Cleanup();
            pawn.ExitMap(false, Rot4.North);
        };
        skip.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return skip;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}
