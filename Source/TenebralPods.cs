using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Shared guard: vanilla despawns a pawn before TryAcceptThing, so a refused pawn is put back instead of vanishing.
public abstract class Building_TenebralCasket : Building_CryptosleepCasket
{
    public abstract AcceptanceReport CanAccept(Pawn pawn);

    public override bool Accepts(Thing thing) =>
        base.Accepts(thing) && !HasAnyContents && thing is Pawn pawn && CanAccept(pawn).Accepted;

    public override bool TryAcceptThing(Thing thing, bool allowSpecialEffects = true)
    {
        if (base.TryAcceptThing(thing, allowSpecialEffects))
        {
            OnAccepted((Pawn)thing);
            return true;
        }
        if (thing is Pawn pawn && !pawn.Spawned && pawn.holdingOwner == null && Spawned)
        {
            GenSpawn.Spawn(pawn, InteractionCell, Map);
        }
        return false;
    }

    protected virtual void OnAccepted(Pawn pawn)
    {
    }

    public Pawn? Occupant => ContainedThing as Pawn;

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
    {
        if (!HasAnyContents)
        {
            AcceptanceReport report = CanAccept(myPawn);
            if (!report.Accepted)
            {
                yield return new FloatMenuOption("CannotUseReason".Translate(report.Reason), null);
                yield break;
            }
        }
        foreach (FloatMenuOption option in base.GetFloatMenuOptions(myPawn))
        {
            yield return option;
        }
    }
}

// Holds a half-crystallized pawn for (100 - crystallized%)% of a year, then releases it as an Umbral. No early exit.
public class Building_UmbralTransformationPod : Building_TenebralCasket
{
    private int releaseTick = -1;

    public override AcceptanceReport CanAccept(Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike)
        {
            return false;
        }
        float needed = TenebralPsychosis.BreakSettings.podThreshold;
        float fraction = Crystallization.Fraction(pawn);
        if (fraction < needed)
        {
            return "BOR_UmbralPodNotCrystallized".Translate(fraction.ToStringPercent(), needed.ToStringPercent());
        }
        return true;
    }

    protected override void OnAccepted(Pawn pawn)
    {
        float remaining = 1f - Crystallization.Fraction(pawn);
        releaseTick = Find.TickManager.TicksGame + Mathf.RoundToInt(remaining * GenDate.TicksPerYear);
    }

    public override void TickRare()
    {
        base.TickRare();
        Pawn? pawn = Occupant;
        if (pawn == null || releaseTick < 0 || Find.TickManager.TicksGame < releaseTick)
        {
            return;
        }
        releaseTick = -1;
        Umbral.MakeUmbral(pawn);
        base.EjectContents();
        if (pawn.Faction == Faction.OfPlayer)
        {
            Find.LetterStack.ReceiveLetter("BOR_UmbralEmergedLabel".Translate(pawn.LabelShortCap),
                "BOR_UmbralEmergedText".Translate(pawn.Named("PAWN")), LetterDefOf.PositiveEvent, pawn);
        }
    }

    // The hold cannot end early; only destruction frees the pawn.
    public override void EjectContents()
    {
        if (releaseTick >= 0 && !Destroyed)
        {
            return;
        }
        base.EjectContents();
    }

    public override bool CanOpen => false;

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (Occupant != null && releaseTick >= 0)
        {
            text += "\n" + "BOR_UmbralPodRemaining".Translate((releaseTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
        }
        return text;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref releaseTick, "releaseTick", -1);
    }
}

public enum TenebralPodMode
{
    Stasis,
    Healing,
    Operating
}

// Bionic installator, healing pod and stasis pod in one.
public class Building_TenebralPod : Building_TenebralCasket
{
    private const int HealInterval = 60;
    private const float HealingMultiplier = 10f;
    private const float NaturalHealPerDay = 8f;

    private TenebralPodMode mode = TenebralPodMode.Stasis;
    private ThingOwner<Thing> ingredients;
    private RecipeDef? operation;
    private BodyPartRecord? operationPart;
    private float operationProgress;

    public Building_TenebralPod()
    {
        ingredients = new ThingOwner<Thing>(this);
    }

    public TenebralPodMode Mode => mode;

    public RecipeDef? Operation => operation;

    public static bool HasTenebralHediff(Pawn pawn) =>
        TenebralMurder.ImplantCount(pawn) > 0 || pawn.health.hediffSet.HasHediff(BOR_TenebralDefOf.BOR_TenebralPsychosis);

    public override AcceptanceReport CanAccept(Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike)
        {
            return false;
        }
        if (mode == TenebralPodMode.Healing && !HasTenebralHediff(pawn))
        {
            return "BOR_TenebralPodNeedsTenebral".Translate();
        }
        return true;
    }

    // Operation recipes: anything installed from a tenebral organ or appendage. Not RecipeDef.IsSurgery, which is
    // only true for recipes linked to a race and so misses most of the tenebral installs.
    public static bool IsTenebralOperation(RecipeDef recipe) =>
        recipe.addsHediff != null
        && recipe.Worker is Recipe_InstallArtificialBodyPart or Recipe_InstallNaturalBodyPart or Recipe_InstallImplant
        && recipe.ingredients.Any(i => i.filter.Allows(BOR_TenebralDefOf.BOR_TenebralOrgan) || i.filter.Allows(BOR_TenebralDefOf.BOR_TenebralAppendage));

    private static List<RecipeDef>? operations;

    public static List<RecipeDef> Operations => operations ??= DefDatabase<RecipeDef>.AllDefs.Where(IsTenebralOperation).OrderBy(r => r.label).ToList();

    // With nobody inside, parts come from the human body the recipe targets, so the operation can be set up in advance.
    private static IEnumerable<BodyPartRecord> TemplateParts(RecipeDef recipe) =>
        BodyDefOf.Human.AllParts.Where(p => recipe.appliedOnFixedBodyParts.Contains(p.def)
            || (p.groups != null && recipe.appliedOnFixedBodyPartGroups.Any(g => p.groups.Contains(g))));

    private static IEnumerable<BodyPartRecord> PartsFor(RecipeDef recipe, Pawn? pawn) =>
        pawn == null ? TemplateParts(recipe) : recipe.Worker.GetPartsToApplyOn(pawn, recipe);

    // Whether the chosen part can take the operation on this pawn (already has it, missing parent, crystallized...).
    private bool OperationFits(Pawn pawn) =>
        operation != null && operationPart != null && operation.Worker.GetPartsToApplyOn(pawn, operation).Contains(operationPart);

    // The organ/appendage and femtoclusters the operation needs; other surgery ingredients are not used by the pod.
    public static IEnumerable<ThingDefCountClass> Requirements(RecipeDef recipe)
    {
        ThingDef[] allowed = { BOR_TenebralDefOf.BOR_TenebralOrgan, BOR_TenebralDefOf.BOR_TenebralAppendage, BOR_TenebralDefOf.BOR_Femtoclusters };
        foreach (IngredientCount ing in recipe.ingredients)
        {
            ThingDef? def = allowed.FirstOrDefault(d => ing.filter.Allows(d));
            if (def != null)
            {
                yield return new ThingDefCountClass(def, Mathf.Max(1, Mathf.RoundToInt(ing.GetBaseCount())));
            }
        }
    }

    public int Missing(ThingDef def)
    {
        if (operation == null)
        {
            return 0;
        }
        int needed = Requirements(operation).Where(r => r.thingDef == def).Sum(r => r.count);
        return Mathf.Max(0, needed - ingredients.TotalStackCountOfDef(def));
    }

    public IEnumerable<ThingDefCountClass> MissingIngredients()
    {
        if (operation == null)
        {
            yield break;
        }
        foreach (ThingDefCountClass r in Requirements(operation))
        {
            int missing = Missing(r.thingDef);
            if (missing > 0)
            {
                yield return new ThingDefCountClass(r.thingDef, missing);
            }
        }
    }

    public bool WantsIngredients => mode == TenebralPodMode.Operating && operation != null && MissingIngredients().Any();

    public void AddIngredient(Thing thing)
    {
        int take = Mathf.Min(thing.stackCount, Missing(thing.def));
        if (take <= 0)
        {
            return;
        }
        Thing part = thing.SplitOff(take);
        if (!ingredients.TryAdd(part))
        {
            GenPlace.TryPlaceThing(part, InteractionCell, Map, ThingPlaceMode.Near);
        }
    }

    private void DropIngredients()
    {
        if (Spawned)
        {
            ingredients.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
        }
    }

    private void SetMode(TenebralPodMode newMode)
    {
        mode = newMode;
        if (newMode != TenebralPodMode.Operating)
        {
            CancelOperation();
        }
    }

    private void CancelOperation()
    {
        operation = null;
        operationPart = null;
        operationProgress = 0f;
        DropIngredients();
    }

    public override void TickRare()
    {
        base.TickRare();
        Pawn? pawn = Occupant;
        if (pawn == null || mode != TenebralPodMode.Operating || operation == null || operationPart == null || MissingIngredients().Any() || !OperationFits(pawn))
        {
            return;
        }
        // One step, as long as the recipe's own work amount.
        operationProgress += GenTicks.TickRareInterval;
        if (operationProgress >= operation.workAmount)
        {
            Install(pawn);
        }
    }

    protected override void Tick()
    {
        base.Tick();
        if (mode == TenebralPodMode.Healing && this.IsHashIntervalTick(HealInterval) && Occupant is Pawn pawn)
        {
            Heal(pawn);
        }
    }

    // Each injury heals at ten times the natural rate, so more injuries heal more in total; the pod opens when done.
    private void Heal(Pawn pawn)
    {
        List<Hediff_Injury> injuries = new();
        pawn.health.hediffSet.GetHediffs(ref injuries, h => h.CanHealNaturally() || h.Bleeding);
        if (injuries.Count == 0)
        {
            EjectContents();
            return;
        }
        float amount = NaturalHealPerDay * HealingMultiplier * pawn.GetStatValue(StatDefOf.InjuryHealingFactor) * HealInterval / GenDate.TicksPerDay;
        foreach (Hediff_Injury injury in injuries)
        {
            if (!injury.IsTended())
            {
                injury.Tended(1f, 1f);
            }
            injury.Heal(amount);
        }
    }

    private void Install(Pawn pawn)
    {
        RecipeDef recipe = operation!;
        BodyPartRecord part = operationPart!;
        if (recipe.addsHediff.hediffClass != null && typeof(Hediff_AddedPart).IsAssignableFrom(recipe.addsHediff.hediffClass))
        {
            MedicalRecipesUtility.RestorePartAndSpawnAllPreviousParts(pawn, part, InteractionCell, Map);
        }
        pawn.health.AddHediff(recipe.addsHediff, part);
        ingredients.ClearAndDestroyContents();
        operation = null;
        operationPart = null;
        operationProgress = 0f;
        Messages.Message("BOR_TenebralPodInstalled".Translate(pawn.LabelShortCap, recipe.addsHediff.label), this, MessageTypeDefOf.PositiveEvent);
        EjectContents();
    }

    // Recipe first, then the part. Works with or without someone inside.
    private void ChooseOperation()
    {
        Pawn? pawn = Occupant;
        List<FloatMenuOption> options = new();
        foreach (RecipeDef recipe in Operations)
        {
            if (!recipe.AvailableNow)
            {
                continue;
            }
            List<BodyPartRecord> parts = PartsFor(recipe, pawn).ToList();
            if (parts.Count == 0)
            {
                continue;
            }
            RecipeDef r = recipe;
            options.Add(new FloatMenuOption(r.LabelCap, () => ChoosePart(r, parts)));
        }
        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("BOR_TenebralPodNoOperations".Translate(), null));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void ChoosePart(RecipeDef recipe, List<BodyPartRecord> parts)
    {
        List<FloatMenuOption> options = new();
        foreach (BodyPartRecord part in parts)
        {
            BodyPartRecord p = part;
            options.Add(new FloatMenuOption(p.LabelCap, () =>
            {
                if (operation != recipe)
                {
                    CancelOperation();
                }
                operation = recipe;
                operationPart = p;
                operationProgress = 0f;
            }));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo g in base.GetGizmos())
        {
            yield return g;
        }
        if (Faction != Faction.OfPlayer)
        {
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "BOR_TenebralPodMode".Translate(("BOR_TenebralPodMode_" + mode).Translate()),
            defaultDesc = "BOR_TenebralPodModeDesc".Translate(),
            icon = def.uiIcon,
            action = () =>
            {
                List<FloatMenuOption> options = new();
                foreach (TenebralPodMode m in System.Enum.GetValues(typeof(TenebralPodMode)))
                {
                    TenebralPodMode chosen = m;
                    options.Add(new FloatMenuOption(("BOR_TenebralPodMode_" + chosen).Translate(), () => SetMode(chosen)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        if (mode != TenebralPodMode.Operating)
        {
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "BOR_TenebralPodChoose".Translate(),
            defaultDesc = "BOR_TenebralPodChooseDesc".Translate(),
            icon = BOR_TenebralDefOf.BOR_TenebralOrgan.uiIcon,
            action = ChooseOperation
        };
        if (operation != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "BOR_TenebralPodCancel".Translate(),
                defaultDesc = "BOR_TenebralPodCancelDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel"),
                action = CancelOperation
            };
        }
    }

    // Only stasis is real cryptosleep; leaving after healing or an operation gives no cryptosleep sickness.
    // The chosen operation and its loaded ingredients stay for the next occupant.
    public override void EjectContents()
    {
        List<Pawn> pawns = innerContainer.OfType<Pawn>().ToList();
        base.EjectContents();
        operationProgress = 0f;
        if (mode == TenebralPodMode.Stasis)
        {
            return;
        }
        foreach (Pawn pawn in pawns)
        {
            Umbral.RemoveHediffs(pawn, HediffDefOf.CryptosleepSickness);
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        DropIngredients();
        base.Destroy(mode);
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        text += "\n" + "BOR_TenebralPodModeInspect".Translate(("BOR_TenebralPodMode_" + mode).Translate());
        if (operation != null && operationPart != null)
        {
            text += "\n" + "BOR_TenebralPodOperation".Translate(operation.LabelCap, operationPart.LabelCap,
                (operationProgress / Mathf.Max(1f, operation.workAmount)).ToStringPercent());
            string missing = MissingIngredients().Select(r => r.Label).ToCommaList();
            if (!missing.NullOrEmpty())
            {
                text += "\n" + "BOR_TenebralPodMissing".Translate(missing);
            }
            if (Occupant is Pawn pawn && !OperationFits(pawn))
            {
                text += "\n" + "BOR_TenebralPodDoesNotFit".Translate(pawn.LabelShortCap);
            }
            else if (Occupant == null)
            {
                text += "\n" + "BOR_TenebralPodWaitingForPatient".Translate();
            }
        }
        return text;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref mode, "podMode");
        Scribe_Deep.Look(ref ingredients, "ingredients", this);
        Scribe_Defs.Look(ref operation, "operation");
        Scribe_BodyParts.Look(ref operationPart, "operationPart");
        Scribe_Values.Look(ref operationProgress, "operationProgress");
        ingredients ??= new ThingOwner<Thing>(this);
    }
}

// Carries a tenebral organ, appendage or femtoclusters into a Tenebral Pod waiting on an operation.
public class JobDriver_LoadTenebralPod : JobDriver
{
    private const TargetIndex ItemInd = TargetIndex.A;
    private const TargetIndex PodInd = TargetIndex.B;

    private Building_TenebralPod Pod => (Building_TenebralPod)job.GetTarget(PodInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.GetTarget(ItemInd), job, 1, job.count, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(PodInd);
        this.FailOn(() => !Pod.WantsIngredients);
        yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(ItemInd);
        yield return Toils_Haul.StartCarryThing(ItemInd, subtractNumTakenFromJobCount: true);
        yield return Toils_Goto.GotoThing(PodInd, PathEndMode.InteractionCell);
        Toil deposit = ToilMaker.MakeToil("LoadTenebralPod");
        deposit.initAction = () =>
        {
            Thing? carried = pawn.carryTracker.CarriedThing;
            if (carried == null)
            {
                return;
            }
            Pod.AddIngredient(carried);
            if (!carried.Destroyed && carried.stackCount > 0)
            {
                pawn.carryTracker.TryDropCarriedThing(pawn.Position, ThingPlaceMode.Near, out _);
            }
        };
        deposit.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return deposit;
    }
}

public class WorkGiver_LoadTenebralPod : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            if (b is Building_TenebralPod pod && pod.WantsIngredients)
            {
                yield return pod;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !PotentialWorkThingsGlobal(pawn).Any();

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_TenebralPod pod || !pod.WantsIngredients || t.IsForbidden(pawn) || !pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Some))
        {
            return null;
        }
        foreach (ThingDefCountClass need in pod.MissingIngredients())
        {
            Thing? item = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(need.thingDef), PathEndMode.ClosestTouch,
                TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced));
            if (item == null)
            {
                continue;
            }
            Job job = JobMaker.MakeJob(BOR_TenebralDefOf.BOR_LoadTenebralPod, item, pod);
            job.count = Mathf.Min(need.count, item.stackCount);
            return job;
        }
        return null;
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class TenebralPodPatches
{
    // Vanilla picks the first casket def with a free casket; skip pods that would refuse the pawn.
    [HarmonyPatch(typeof(Building_CryptosleepCasket), nameof(Building_CryptosleepCasket.FindCryptosleepCasketFor))]
    public static class FindCryptosleepCasketFor_Patch
    {
        public static void Postfix(Pawn p, Pawn traveler, bool ignoreOtherReservations, ref Building_CryptosleepCasket? __result)
        {
            if (__result == null || __result.Accepts(p))
            {
                return;
            }
            __result = null;
            float best = float.MaxValue;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(d => d.IsCryptosleepCasket))
            {
                foreach (Thing t in p.MapHeld.listerThings.ThingsOfDef(def))
                {
                    if (t is not Building_CryptosleepCasket casket || casket.HasAnyContents || !casket.Accepts(p)
                        || !traveler.CanReserveAndReach(casket, PathEndMode.InteractionCell, Danger.Deadly, 1, -1, null, ignoreOtherReservations))
                    {
                        continue;
                    }
                    float dist = casket.Position.DistanceToSquared(p.PositionHeld);
                    if (dist < best)
                    {
                        best = dist;
                        __result = casket;
                    }
                }
            }
        }
    }
}
