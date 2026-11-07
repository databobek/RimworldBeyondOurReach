using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Tunables for the Righteous Slaughter's blessing; placed on BOR_SlaughterBlessing.
public class DefModExtension_RighteousSlaughter : DefModExtension
{
    // Abilities recover this many times faster, so a cooldown lasts 1 / cooldownSpeed as long.
    public float cooldownSpeed = 1.5f;

    // Umbral transformation progresses this many times faster.
    public float transformationSpeed = 1.25f;
}

// Ordered from the butcher table, alongside component harvesting; see CompComponentHarvest.
public static class RighteousSlaughter
{
    public static DefModExtension_RighteousSlaughter Settings =>
        BOR_UmbralDefOf.BOR_SlaughterBlessing.GetModExtension<DefModExtension_RighteousSlaughter>() ?? new DefModExtension_RighteousSlaughter();

    public static int BlessingTicks =>
        BOR_UmbralDefOf.BOR_SlaughterBlessing.CompProps<HediffCompProperties_Disappears>()?.disappearsAfterTicks.max ?? 0;

    private static IEnumerable<Pawn> ColonyUmbrals =>
        PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction.Where(p => p.RaceProps.Humanlike && Umbral.IsUmbral(p));

    public static bool AnyColonyUmbrals => ColonyUmbrals.Any();

    public static bool IsBlessed(Pawn pawn) => pawn.health?.hediffSet?.HasHediff(BOR_UmbralDefOf.BOR_SlaughterBlessing) ?? false;

    public static bool CanBeVictim(Pawn pawn) =>
        !pawn.Dead && pawn.RaceProps.Humanlike && pawn.IsPrisonerOfColony && !pawn.InAggroMentalState && !PrisonBreakUtility.IsPrisonBreaking(pawn);

    // Limbs only, from the outside in, so the victim lives until the final cut. Fingers and toes go with their hand or foot.
    public static void TearLimb(Pawn victim)
    {
        List<BodyPartRecord> limbs = victim.health.hediffSet.GetNotMissingParts().Where(IsLimb).ToList();
        if (!limbs.Where(p => !p.parts.Any(limbs.Contains)).TryRandomElement(out BodyPartRecord part))
        {
            return;
        }
        Hediff_MissingPart torn = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, victim, part);
        torn.lastInjury = HediffDefOf.Cut;
        torn.IsFresh = true;
        victim.health.AddHediff(torn);
        victim.health.DropBloodFilth();
    }

    private static bool IsLimb(BodyPartRecord part)
    {
        List<BodyPartTagDef> tags = part.def.tags;
        return tags.Contains(BodyPartTagDefOf.ManipulationLimbCore) || tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment)
            || tags.Contains(BodyPartTagDefOf.MovingLimbCore) || tags.Contains(BodyPartTagDefOf.MovingLimbSegment);
    }

    // Another slaughter restarts the blessing; it doesn't stack. Returns how many Umbrals were blessed.
    public static int Bless()
    {
        int count = 0;
        foreach (Pawn pawn in ColonyUmbrals.ToList())
        {
            Umbral.RemoveHediffs(pawn, BOR_UmbralDefOf.BOR_SlaughterBlessing);
            pawn.health.AddHediff(BOR_UmbralDefOf.BOR_SlaughterBlessing);
            count++;
        }
        return count;
    }
}

public class JobDriver_RighteousSlaughter : JobDriver_HarvestComponents
{
    private const TargetIndex VictimInd = TargetIndex.C;
    private const int MessRadius = 6;
    private const int TearIntervalTicks = 75;

    private Pawn? Victim => job.GetTarget(VictimInd).Thing as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (Table == null || Victim == null)
        {
            return false;
        }
        return pawn.Reserve(Table, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(Victim, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TableInd);
        this.FailOn(() => Victim == null || !RighteousSlaughter.CanBeVictim(Victim));
        this.AddFinishAction(cond =>
        {
            if (pawn.Spawned && pawn.carryTracker?.CarriedThing != null)
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            }
            if (Victim != null && !Victim.Dead)
            {
                Umbral.RemoveHediffs(Victim, BOR_UmbralDefOf.BOR_SlaughterRestraint);
            }
        });

        yield return Toils_Goto.GotoThing(VictimInd, PathEndMode.ClosestTouch)
            .FailOnDespawnedOrNull(VictimInd).FailOnSomeonePhysicallyInteracting(VictimInd);

        // A pinned prisoner is downed, so it can be carried and stays on the table.
        Toil pin = ToilMaker.MakeToil("RighteousSlaughterPin");
        pin.initAction = () => Victim?.health.AddHediff(BOR_UmbralDefOf.BOR_SlaughterRestraint);
        pin.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return pin;

        yield return Toils_Haul.StartCarryThing(VictimInd);
        yield return Toils_Goto.GotoThing(TableInd, PathEndMode.InteractionCell).FailOn(() => pawn.carryTracker.CarriedThing == null);
        yield return DropCarried(VictimInd);

        Toil work = WorkToil(() => Victim != null && Victim.Spawned);
        work.AddPreTickAction(() =>
        {
            if (ticksDone > 0 && ticksDone % TearIntervalTicks == 0 && Victim != null && Victim.Spawned)
            {
                RighteousSlaughter.TearLimb(Victim);
            }
        });
        yield return work;

        Toil finish = ToilMaker.MakeToil("RighteousSlaughterFinish");
        finish.initAction = Finish;
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    protected override ThingDef PickFilth() =>
        Rand.Chance(0.3f) ? BOR_HarvestDefOf.BOR_Filth_Feces : base.PickFilth();


    private void Finish()
    {
        Thing? table = Table;
        Pawn? victim = Victim;
        if (table == null || !table.Spawned || victim == null || !victim.Spawned)
        {
            EndJobWith(JobCondition.Incompletable);
            return;
        }
        string victimLabel = victim.LabelShort;
        // The same calls as a vanilla execution, so it counts as one for precepts and thoughts.
        ExecutionUtility.DoExecutionByCut(pawn, victim);
        ThoughtUtility.GiveThoughtsForPawnExecuted(victim, pawn, PawnExecutionKind.GenericBrutal);
        TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, victim);
        victim.Corpse?.Destroy();
        Thing skull = ThingMaker.MakeThing(ThingDefOf.Skull);
        skull.TryGetComp<CompHasSources>()?.AddSource(victimLabel);
        GenPlace.TryPlaceThing(skull, table.InteractionCell, table.Map, ThingPlaceMode.Near);
        for (int i = 0; i < 40; i++)
        {
            Splatter(MessRadius);
        }
        table.TryGetComp<CompComponentHarvest>()?.StartSlaughterCooldown();
        Messages.Message("BOR_Slaughter_Done".Translate(RighteousSlaughter.Bless()), table, MessageTypeDefOf.PositiveEvent);
        ApplyThoughts();
    }
}
