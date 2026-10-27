using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class JobDriver_HarvestComponents : JobDriver
{
    private const TargetIndex TableInd = TargetIndex.A;
    private const TargetIndex PicoInd = TargetIndex.B;
    private const TargetIndex CorpseInd = TargetIndex.C;
    private const int WorkTicks = 600;
    private const int BloodRadius = 3;

    private int ticksDone;

    private Thing? Table => job.GetTarget(TableInd).Thing;
    private Thing? PicoThing => job.GetTarget(PicoInd).Thing;
    private Corpse? CorpseThing => job.GetTarget(CorpseInd).Thing as Corpse;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (Table == null || PicoThing == null || CorpseThing == null)
        {
            return false;
        }
        return pawn.Reserve(Table, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(job.GetTarget(PicoInd), job, 1, 1, null, errorOnFailed)
            && pawn.Reserve(job.GetTarget(CorpseInd), job, 1, 1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TableInd);
        this.FailOnDestroyedOrNull(CorpseInd);
        this.AddFinishAction(cond =>
        {
            if (pawn.Spawned && pawn.carryTracker?.CarriedThing != null)
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            }
        });

        yield return Toils_Goto.GotoThing(PicoInd, PathEndMode.ClosestTouch)
            .FailOnDestroyedNullOrForbidden(PicoInd).FailOnSomeonePhysicallyInteracting(PicoInd);
        yield return Toils_Haul.StartCarryThing(PicoInd).FailOnDestroyedNullOrForbidden(PicoInd);
        yield return Toils_Goto.GotoThing(TableInd, PathEndMode.InteractionCell).FailOn(() => pawn.carryTracker.CarriedThing == null);
        yield return DropCarried(null);

        yield return Toils_Goto.GotoThing(CorpseInd, PathEndMode.ClosestTouch)
            .FailOnDestroyedNullOrForbidden(CorpseInd).FailOnSomeonePhysicallyInteracting(CorpseInd).FailOn(() => !CorpseOk() || !PicoOnTable());
        yield return Toils_Haul.StartCarryThing(CorpseInd).FailOnDestroyedNullOrForbidden(CorpseInd);
        yield return Toils_Goto.GotoThing(TableInd, PathEndMode.InteractionCell).FailOn(() => pawn.carryTracker.CarriedThing == null || !PicoOnTable());
        yield return DropCarried(CorpseInd);

        Toil work = ToilMaker.MakeToil("HarvestComponentsWork");
        work.initAction = () => ticksDone = 0;
        work.tickAction = () =>
        {
            ticksDone++;
            if (Table != null)
            {
                pawn.rotationTracker.FaceTarget(Table);
            }
            if (ticksDone % 4 == 0)
            {
                Splatter();
            }
            pawn.skills?.Learn(SkillDefOf.Crafting, 0.05f);
            if (ticksDone >= WorkTicks)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.FailOn(() => !CorpseOk() || !CorpseOnTable() || !PicoOnTable());
        work.WithProgressBar(TableInd, () => (float)ticksDone / WorkTicks);
        work.activeSkill = () => SkillDefOf.Crafting;
        yield return work;

        Toil finish = ToilMaker.MakeToil("HarvestComponentsFinish");
        finish.initAction = Finish;
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    private bool PicoOnTable()
    {
        Thing? pico = PicoThing;
        return pico != null && !pico.Destroyed && pico.Spawned && pico.def == BOR_HarvestDefOf.BOR_Picoclusters;
    }

    private bool CorpseOnTable()
    {
        Corpse? corpse = CorpseThing;
        return corpse != null && !corpse.Destroyed && corpse.Spawned && corpse.InnerPawn != null;
    }

    private bool CorpseOk()
    {
        Corpse? corpse = CorpseThing;
        CompComponentHarvest? comp = Table?.TryGetComp<CompComponentHarvest>();
        if (corpse == null || corpse.Destroyed || corpse.InnerPawn == null || comp == null)
        {
            return false;
        }
        // The corpse is carried (unspawned) while hauled; only the health/rot conditions matter then.
        return corpse.GetRotStage() == RotStage.Fresh
            && corpse.HitPoints >= comp.Props.minCorpseHealthPct * corpse.MaxHitPoints;
    }

    private Toil DropCarried(TargetIndex? ind)
    {
        Toil toil = ToilMaker.MakeToil("HarvestComponentsDrop");
        toil.initAction = () =>
        {
            Thing? carried = pawn.carryTracker.CarriedThing;
            Thing? table = Table;
            if (carried == null || table == null || !table.Spawned)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (!pawn.carryTracker.TryDropCarriedThing(table.InteractionCell, ThingPlaceMode.Near, out Thing placed) || placed == null)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            // The dropped thing may be a merged stack, so keep the job pointing at what is actually on the ground.
            job.SetTarget(ind ?? PicoInd, placed);
        };
        toil.defaultCompleteMode = ToilCompleteMode.Instant;
        return toil;
    }

    private void Splatter()
    {
        Thing? table = Table;
        Map? map = table?.Map;
        if (table == null || map == null)
        {
            return;
        }
        foreach (IntVec3 c in GenRadial.RadialCellsAround(table.Position, BloodRadius, true))
        {
            if (c.InBounds(map) && Rand.Chance(0.35f))
            {
                FilthMaker.TryMakeFilth(c, map, Rand.Chance(0.2f) ? ThingDefOf.Filth_CorpseBile : ThingDefOf.Filth_Blood, Rand.RangeInclusive(1, 3));
            }
        }
    }

    private void Finish()
    {
        Thing? table = Table;
        Map? map = table?.Map;
        Corpse? corpse = CorpseThing;
        Thing? pico = PicoThing;
        // Abort without consuming anything if any participant vanished at the last moment.
        if (table == null || map == null || !table.Spawned || !CorpseOnTable() || !PicoOnTable() || corpse == null || pico == null)
        {
            EndJobWith(JobCondition.Incompletable);
            return;
        }
        CompComponentHarvest? comp = table.TryGetComp<CompComponentHarvest>();
        IntVec3 pos = table.InteractionCell;

        pico.SplitOff(1).Destroy();
        corpse.Destroy();

        Thing components = ThingMaker.MakeThing(BOR_HarvestDefOf.BOR_ComponentIII);
        components.stackCount = comp?.Props.componentCount ?? 10;
        GenPlace.TryPlaceThing(components, pos, map, ThingPlaceMode.Near);

        for (int i = 0; i < 40; i++)
        {
            Splatter();
        }

        comp?.StartCooldown();
        ApplyThoughts();
    }

    private void ApplyThoughts()
    {
        MemoryThoughtHandler? memories = pawn.needs?.mood?.thoughts?.memories;
        ThoughtDef? ownThought = OwnThoughtFor(pawn);
        if (memories != null && ownThought != null)
        {
            Thought_Memory? existing = memories.GetFirstMemoryOfDef(ownThought);
            int stage = existing == null
                ? 0
                : System.Math.Min(existing.CurStageIndex + 1, ownThought.stages.Count - 1);
            memories.TryGainMemory((Thought_Memory)ThoughtMaker.MakeThought(ownThought, stage));
        }
        if (pawn.Map == null)
        {
            return;
        }
        foreach (Pawn other in pawn.Map.mapPawns.FreeColonistsSpawned)
        {
            if (other != pawn)
            {
                ThoughtDef? opinion = OpinionThoughtFor(other);
                if (opinion != null)
                {
                    other.needs?.mood?.thoughts?.memories?.TryGainMemory(opinion, pawn);
                }
            }
        }
    }

    /// <summary>
    /// Ideology's biomass harvest precept decides the operator's own mood memory:
    /// exalted flips it positive, neutral removes it, abhorrent (or no ideology) keeps the base thought.
    /// </summary>
    private static ThoughtDef? OwnThoughtFor(Pawn p)
    {
        if (!ModsConfig.IdeologyActive || p.Ideo == null)
        {
            return BOR_HarvestDefOf.BOR_MutilatedBody;
        }
        if (HasPrecept(p, "BOR_BiomassHarvest_Exalted"))
        {
            return DefDatabase<ThoughtDef>.GetNamedSilentFail("BOR_MutilatedBodyExalted") ?? BOR_HarvestDefOf.BOR_MutilatedBody;
        }
        if (HasPrecept(p, "BOR_BiomassHarvest_Neutral"))
        {
            return null;
        }
        return BOR_HarvestDefOf.BOR_MutilatedBody;
    }

    /// <summary>The observer's own ideo decides how they judge the operator, same tiers as <see cref="OwnThoughtFor"/>.</summary>
    private static ThoughtDef? OpinionThoughtFor(Pawn observer)
    {
        if (!ModsConfig.IdeologyActive || observer.Ideo == null)
        {
            return BOR_HarvestDefOf.BOR_MutilatedBodySocial;
        }
        if (HasPrecept(observer, "BOR_BiomassHarvest_Exalted"))
        {
            return DefDatabase<ThoughtDef>.GetNamedSilentFail("BOR_MutilatedBodySocialExalted") ?? BOR_HarvestDefOf.BOR_MutilatedBodySocial;
        }
        if (HasPrecept(observer, "BOR_BiomassHarvest_Neutral"))
        {
            return null;
        }
        return BOR_HarvestDefOf.BOR_MutilatedBodySocial;
    }

    private static bool HasPrecept(Pawn p, string defName)
    {
        PreceptDef? def = DefDatabase<PreceptDef>.GetNamedSilentFail(defName);
        return def != null && p.Ideo != null && p.Ideo.HasPrecept(def);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}
