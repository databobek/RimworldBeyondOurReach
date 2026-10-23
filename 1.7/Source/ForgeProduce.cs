using System.Collections.Generic;
using RimWorld;
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
        this.FailOn(() => Comp == null || !Comp.PowerOn || Comp.Queued(Slot) <= 0);

        yield return Toils_Goto.GotoThing(ForgeInd, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("ForgeProduce");
        work.initAction = () =>
        {
            ThingDef? def = Comp?.Slots[Slot];
            totalTicks = def == null ? 1 : ForgeMath.ProductionTicks(def, pawn);
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
            ticksDone++;
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
            CompForge? comp = Comp;
            ThingDef? def = comp?.Slots[Slot];
            if (comp == null || def == null)
            {
                return;
            }
            Thing product = ThingMaker.MakeThing(def, def.MadeFromStuff ? (comp.StuffOf(Slot) ?? GenStuff.DefaultStuffFor(def)) : null);
            product.stackCount = ForgeMath.YieldFor(def);
            product.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            comp.ConsumeQueued(Slot);
            GenPlace.TryPlaceThing(product, ForgeThing.InteractionCell, ForgeThing.Map, ThingPlaceMode.Near);
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

public class WorkGiver_ForgeProduce : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            CompForge? comp = b.TryGetComp<CompForge>();
            if (comp != null && comp.NextProductionSlot() >= 0)
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
            if (comp != null && comp.NextProductionSlot() >= 0)
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
        int slot = comp.NextProductionSlot();
        if (slot < 0 || !comp.HasResource)
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
