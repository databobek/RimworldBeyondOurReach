using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class JobDriver_ForgeOverdrive : JobDriver
{
    private const TargetIndex ForgeInd = TargetIndex.A;
    private const int AdjustTicks = 240;

    private int ticksDone;

    private Thing ForgeThing => job.GetTarget(ForgeInd).Thing;
    private CompForge? Comp => ForgeThing?.TryGetComp<CompForge>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(ForgeThing, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(ForgeInd);
        this.FailOn(() => Comp == null || !Comp.PowerOn || !Comp.OverdrivePending);

        yield return Toils_Goto.GotoThing(ForgeInd, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("ForgeOverdrive");
        work.tickAction = () =>
        {
            pawn.rotationTracker.FaceTarget(ForgeThing);
            ticksDone++;
            if (ticksDone >= AdjustTicks)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithProgressBar(ForgeInd, () => (float)ticksDone / AdjustTicks);
        work.handlingFacing = true;
        yield return work;

        Toil apply = ToilMaker.MakeToil("ForgeOverdriveApply");
        apply.initAction = () => Comp?.ApplyOverdriveTarget();
        apply.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return apply;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}

public class WorkGiver_ForgeOverdrive : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            if (b.TryGetComp<CompForge>()?.OverdrivePending == true)
            {
                yield return b;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            if (b.TryGetComp<CompForge>()?.OverdrivePending == true)
            {
                return false;
            }
        }
        return true;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        CompForge? comp = t.TryGetComp<CompForge>();
        if (comp == null || !comp.OverdrivePending || !comp.PowerOn || !comp.IsAssignedOrUnassigned(pawn))
        {
            return null;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        return JobMaker.MakeJob(BOR_ForgeDefOf.BOR_ForgeOverdrive, t);
    }
}
