using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class JobDriver_ForgeAnalyze : JobDriver
{
    private const TargetIndex ForgeInd = TargetIndex.A;
    private const TargetIndex ItemInd = TargetIndex.B;

    private int totalTicks;
    private int ticksDone;

    private Thing ForgeThing => job.GetTarget(ForgeInd).Thing;
    private CompForge? Comp => ForgeThing?.TryGetComp<CompForge>();

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(ForgeThing, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(job.GetTarget(ItemInd), job, 1, 1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(ForgeInd);
        this.FailOn(() => Comp == null || !Comp.PowerOn);
        this.FailOn(() => Comp != null && !Comp.PendingAnalysis.Contains(job.GetTarget(ItemInd).Thing) && pawn.carryTracker.CarriedThing == null);
        this.FailOnDestroyedNullOrForbidden(ItemInd);

        yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch);
        Toil pickUp = ToilMaker.MakeToil("ForgeAnalyzePickUp");
        pickUp.initAction = () =>
        {
            Thing item = job.GetTarget(ItemInd).Thing;
            if (item == null || pawn.carryTracker.TryStartCarry(item, 1) <= 0)
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        pickUp.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return pickUp;
        yield return Toils_Goto.GotoThing(ForgeInd, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("ForgeAnalyze");
        work.initAction = () =>
        {
            Thing? carried = pawn.carryTracker.CarriedThing;
            totalTicks = carried == null ? 1 : ForgeMath.AnalysisTicks(carried.def, pawn);
            ticksDone = 0;
        };
        work.tickAction = () =>
        {
            ticksDone++;
            if (Comp != null)
            {
                Comp.AnalyzingThing = job.GetTarget(ItemInd).Thing;
                Comp.AnalysisProgress = totalTicks <= 0 ? 0f : (float)ticksDone / totalTicks;
            }            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.08f);
            if (ticksDone >= totalTicks)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.AddFinishAction(() =>
        {
            if (Comp != null)
            {
                Comp.AnalyzingThing = null;
                Comp.AnalysisProgress = 0f;
            }
        });
        work.FailOn(() => pawn.carryTracker.CarriedThing == null);
        work.WithProgressBar(ForgeInd, () => totalTicks <= 0 ? 0f : (float)ticksDone / totalTicks);
        work.activeSkill = () => SkillDefOf.Intellectual;
        work.handlingFacing = true;
        yield return work;

        Toil finish = ToilMaker.MakeToil("ForgeAnalyzeFinish");
        finish.initAction = () =>
        {
            Thing? carried = pawn.carryTracker.CarriedThing;
            CompForge? comp = Comp;
            if (carried == null || comp == null)
            {
                return;
            }
            ThingDef def = carried.def;
            ThingDef? stuff = carried.Stuff;
            Thing? target = job.GetTarget(ItemInd).Thing;
            if (target != null)
            {
                comp.RemovePendingAnalysis(target);
            }
            pawn.carryTracker.DestroyCarriedThing();

            if (Rand.Chance(ForgeMath.AnalysisSuccessChance(pawn, comp.Props.analysisSuccessOffset)))
            {
                comp.Learn(def, stuff);
                Messages.Message("BOR_Forge_AnalysisSuccess".Translate(def.LabelCap), ForgeThing, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("BOR_Forge_AnalysisFailed".Translate(def.LabelCap), ForgeThing, MessageTypeDefOf.NegativeEvent);
            }
        };
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref totalTicks, "totalTicks");
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}

public class WorkGiver_ForgeAnalyze : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            CompForge? comp = b.TryGetComp<CompForge>();
            if (comp != null && comp.PendingAnalysis.Count > 0)
            {
                yield return b;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            CompForge? comp = b.TryGetComp<CompForge>();
            if (comp != null && comp.PendingAnalysis.Count > 0)
            {
                return false;
            }
        }
        return true;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        CompForge? comp = t.TryGetComp<CompForge>();
        if (comp == null || !comp.PowerOn || !comp.IsAssignedOrUnassigned(pawn))
        {
            return null;
        }
        if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        foreach (Thing item in comp.PendingAnalysis)
        {
            if (!item.Spawned || item.IsForbidden(pawn) || !pawn.CanReserveAndReach(item, PathEndMode.ClosestTouch, pawn.NormalMaxDanger(), 1, 1, null, forced))
            {
                continue;
            }
            Job job = JobMaker.MakeJob(BOR_ForgeDefOf.BOR_ForgeAnalyze, t, item);
            job.count = 1;
            return job;
        }
        return null;
    }
}
