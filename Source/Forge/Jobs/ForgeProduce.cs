using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class JobDriver_ForgeProduce : JobDriver
{
    private const TargetIndex ForgeInd = TargetIndex.A;

    private int ticksDone;
    private int totalTicks;

    private Thing ForgeThing => job.GetTarget(ForgeInd).Thing;
    private CompForge? Comp => ForgeThing?.TryGetComp<CompForge>();
    private int Slot => job.count;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(ForgeThing, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(ForgeInd);
        this.FailOn(() => Comp == null || !Comp.PowerOn || !Comp.ShouldProduce(Slot));

        yield return Toils_Goto.GotoThing(ForgeInd, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("ForgeProduce");
        work.initAction = () =>
        {
            ThingDef? def = Comp?.Slots[Slot];
            // Linked facilities (tool cabinets, lathes, ...) raise the forge's bench work speed.
            totalTicks = def == null ? 1 : ForgeMath.ProductionTicks(def, pawn, Comp!.Props.workSpeedFactor * Mathf.Max(0.1f, Comp.parent.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor)));
            ticksDone = 0;
        };
        work.tickAction = () =>
        {
            CompForge? comp = Comp;
            if (comp == null)
            {
                return;
            }
            if (ticksDone % comp.Props.workTicksPerDraw == 0 && !comp.TryDrawResource())
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            pawn.rotationTracker.FaceTarget(ForgeThing);
            ticksDone++;
            comp.NotifyProducing();
            comp.SetProgress(Slot, (float)ticksDone / totalTicks);
            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.08f);
            if (ticksDone >= totalTicks)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.AddFinishAction(() => Comp?.SetProgress(Slot, 0f));
        work.WithProgressBar(ForgeInd, () => totalTicks <= 0 ? 0f : (float)ticksDone / totalTicks);
        work.activeSkill = () => SkillDefOf.Intellectual;
        work.handlingFacing = true;
        yield return work;

        Toil finish = ToilMaker.MakeToil("ForgeProduceFinish");
        finish.initAction = () =>
        {
            Comp?.FinishProduction(Slot);
        };
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksDone, "ticksDone");
        Scribe_Values.Look(ref totalTicks, "totalTicks");
    }
}

public class WorkGiver_ForgeProduce : WorkGiver_ForgeBase
{
    protected override bool HasWork(CompForge comp) => !comp.Props.automatic && comp.NextProductionSlot() >= 0;

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        CompForge? comp = t.TryGetComp<CompForge>();
        if (comp == null || comp.Props.automatic || comp.IsFormatting || !comp.PowerOn || !comp.IsAssignedOrUnassigned(pawn))
        {
            return null;
        }
        int slot = comp.NextProductionSlot();
        if (slot < 0 || !comp.CanStartProduction)
        {
            return null;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        Job job = JobMaker.MakeJob(BOR_ForgeDefOf.BOR_ForgeProduce, t);
        job.count = slot;
        return job;
    }
}
