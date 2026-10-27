using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class JobDriver_ForgeFormat : JobDriver
{
    private const TargetIndex ForgeInd = TargetIndex.A;

    private Thing ForgeThing => job.GetTarget(ForgeInd).Thing;
    private CompForge? Comp => ForgeThing?.TryGetComp<CompForge>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(ForgeThing, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(ForgeInd);
        this.FailOn(() => Comp == null || !Comp.PowerOn);

        yield return Toils_Goto.GotoThing(ForgeInd, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("ForgeFormat");
        work.tickAction = () =>
        {
            CompForge? comp = Comp;
            if (comp == null || !comp.IsFormatting)
            {
                ReadyForNextToil();
                return;
            }
            pawn.rotationTracker.FaceTarget(ForgeThing);
            comp.FormatTick();
            if (!comp.IsFormatting)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithProgressBar(ForgeInd, () => Comp?.FormatCellProgress ?? 0f);
        work.handlingFacing = true;
        yield return work;
    }
}

public class WorkGiver_ForgeFormat : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            if (b.TryGetComp<CompForge>()?.IsFormatting == true)
            {
                yield return b;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            if (b.TryGetComp<CompForge>()?.IsFormatting == true)
            {
                return false;
            }
        }
        return true;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        CompForge? comp = t.TryGetComp<CompForge>();
        if (comp == null || !comp.IsFormatting || !comp.PowerOn || !comp.IsAssignedOrUnassigned(pawn))
        {
            return null;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        return JobMaker.MakeJob(BOR_ForgeDefOf.BOR_ForgeFormat, t);
    }
}
