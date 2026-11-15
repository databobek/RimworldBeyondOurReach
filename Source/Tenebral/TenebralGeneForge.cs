using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BeyondOurReach;

// Gene forge (Biotech): while the pod holds someone in stasis, a doctor can draw one of their genes out into a
// genepack. Each run costs femtoclusters and tears a random body part out of the sleeper, toe or heart alike.
public partial class Building_TenebralPod
{
    public const int ForgeFemtoclustersNeeded = 1;

    private GeneDef? forgeGene;
    private float forgeProgress;
    private int forgeFemtoclusters;

    public GeneDef? ForgeGene => forgeGene;

    public int ForgeFemtoclusters => forgeFemtoclusters;

    public float ForgeFraction => forgeGene == null ? 0f : forgeProgress / ForgeWorkTicks(forgeGene);

    public static int ForgeWorkTicks(GeneDef gene) => GenDate.TicksPerHour * (2 + Mathf.Max(0, gene.biostatCpx));

    public bool GeneForgeActive => ModsConfig.BiotechActive && mode == TenebralPodMode.Stasis && Occupant?.genes != null;

    // BOR's own genes (Umbral, Orientis...) can't be drawn out.
    public static bool IsForgeable(GeneDef gene) => gene.modContentPack != BOR_TenebralDefOf.BOR_Femtoclusters.modContentPack;

    public IEnumerable<GeneDef> ForgeableGenes() =>
        GeneForgeActive ? Occupant!.genes.GenesListForReading.Select(g => g.def).Distinct().Where(IsForgeable) : Enumerable.Empty<GeneDef>();

    private bool ForgeGeneValid() =>
        forgeGene != null && GeneForgeActive && IsForgeable(forgeGene) && Occupant!.genes.GenesListForReading.Any(g => g.def == forgeGene);

    public bool GeneForgeWantsWork => forgeGene != null && ForgeGeneValid();

    public int ForgeFemtoclustersMissing => forgeGene == null ? 0 : Mathf.Max(0, ForgeFemtoclustersNeeded - forgeFemtoclusters);

    // Picking a different gene starts over; femtoclusters already loaded stay for the next run.
    public void SelectForgeGene(GeneDef? gene)
    {
        if (forgeGene != gene)
        {
            forgeProgress = 0f;
        }
        forgeGene = gene;
    }

    public void DepositFemtoclusters(Thing thing)
    {
        forgeFemtoclusters += thing.stackCount;
        thing.Destroy();
    }

    // Called from RareStep: the sleeper left, the mode changed or the gene is gone.
    private void CheckGeneForge()
    {
        if (forgeGene != null && !ForgeGeneValid())
        {
            SelectForgeGene(null);
        }
    }

    // Returns true when the genepack was made on this tick.
    public bool WorkGeneForge(float amount)
    {
        if (!GeneForgeWantsWork || ForgeFemtoclustersMissing > 0)
        {
            return false;
        }
        forgeProgress += amount;
        if (forgeProgress < ForgeWorkTicks(forgeGene!))
        {
            return false;
        }
        FinishGeneForge();
        return true;
    }

    private void FinishGeneForge()
    {
        GeneDef gene = forgeGene!;
        Pawn pawn = Occupant!;
        forgeGene = null;
        forgeProgress = 0f;
        forgeFemtoclusters -= ForgeFemtoclustersNeeded;

        CountExtraction(pawn);

        Genepack pack = (Genepack)ThingMaker.MakeThing(ThingDefOf.Genepack);
        pack.Initialize(new List<GeneDef> { gene });
        GenPlace.TryPlaceThing(pack, InteractionCell, Map, ThingPlaceMode.Near);

        BodyPartRecord? part = pawn.health.hediffSet.GetNotMissingParts()
            .Where(p => p != pawn.RaceProps.body.corePart).RandomElementWithFallback();
        if (part == null)
        {
            Messages.Message("BOR_GeneForgeDoneNoPart".Translate(gene.LabelCap, pawn.LabelShortCap), pack, MessageTypeDefOf.NeutralEvent);
            return;
        }
        string partLabel = part.Label;
        Hediff_MissingPart missing = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, part);
        missing.lastInjury = HediffDefOf.SurgicalCut;
        pawn.health.AddHediff(missing, part);
        if (pawn.Dead)
        {
            Messages.Message("BOR_GeneForgeDoneKilled".Translate(gene.LabelCap, pawn.LabelShortCap, partLabel), pack, MessageTypeDefOf.NegativeEvent);
        }
        else
        {
            Messages.Message("BOR_GeneForgeDone".Translate(gene.LabelCap, pawn.LabelShortCap, partLabel), pack, MessageTypeDefOf.NeutralEvent);
        }
    }

    // Hidden hediff counting extractions; its stage picks the mood (1-2, 3-5, 6+).
    private static void CountExtraction(Pawn pawn)
    {
        Hediff? counter = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_TenebralDefOf.BOR_GeneForgeExtracted);
        if (counter == null)
        {
            pawn.health.AddHediff(BOR_TenebralDefOf.BOR_GeneForgeExtracted);
        }
        else
        {
            counter.Severity += 1f;
        }
    }

    private void DropForgeFemtoclusters()
    {
        if (forgeFemtoclusters <= 0 || !Spawned)
        {
            return;
        }
        Thing femto = ThingMaker.MakeThing(BOR_TenebralDefOf.BOR_Femtoclusters);
        femto.stackCount = forgeFemtoclusters;
        GenPlace.TryPlaceThing(femto, InteractionCell, Map, ThingPlaceMode.Near);
        forgeFemtoclusters = 0;
    }

    private string GeneForgeInspect()
    {
        if (forgeGene == null)
        {
            return "";
        }
        string text = "\n" + "BOR_GeneForgeInspect".Translate(forgeGene.LabelCap, ForgeFraction.ToStringPercent());
        if (ForgeFemtoclustersMissing > 0)
        {
            text += "\n" + "BOR_TenebralPodMissing".Translate(BOR_TenebralDefOf.BOR_Femtoclusters.label);
        }
        return text;
    }

    private void ExposeGeneForge()
    {
        Scribe_Defs.Look(ref forgeGene, "forgeGene");
        Scribe_Values.Look(ref forgeProgress, "forgeProgress");
        Scribe_Values.Look(ref forgeFemtoclusters, "forgeFemtoclusters");
    }
}

// Brings femtoclusters if the pod lacks them, then works the forge until the genepack comes out.
public class JobDriver_GeneForge : JobDriver
{
    private const TargetIndex PodInd = TargetIndex.A;
    private const TargetIndex FemtoInd = TargetIndex.B;

    private Building_TenebralPod Pod => (Building_TenebralPod)job.GetTarget(PodInd).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(job.GetTarget(PodInd), job, 1, -1, null, errorOnFailed))
        {
            return false;
        }
        return !job.GetTarget(FemtoInd).IsValid || pawn.Reserve(job.GetTarget(FemtoInd), job, 1, job.count, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(PodInd);
        this.FailOn(() => !Pod.GeneForgeWantsWork);
        if (job.GetTarget(FemtoInd).IsValid)
        {
            yield return Toils_Goto.GotoThing(FemtoInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(FemtoInd);
            yield return Toils_Haul.StartCarryThing(FemtoInd, subtractNumTakenFromJobCount: true);
            yield return Toils_Goto.GotoThing(PodInd, PathEndMode.InteractionCell);
            Toil deposit = ToilMaker.MakeToil("GeneForgeDeposit");
            deposit.initAction = () =>
            {
                Thing? carried = pawn.carryTracker.CarriedThing;
                if (carried != null)
                {
                    pawn.carryTracker.innerContainer.Remove(carried);
                    Pod.DepositFemtoclusters(carried);
                }
            };
            deposit.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return deposit;
        }
        yield return Toils_Goto.GotoThing(PodInd, PathEndMode.InteractionCell);
        Toil work = ToilMaker.MakeToil("GeneForgeWork");
        work.tickAction = () =>
        {
            pawn.rotationTracker.FaceTarget(Pod);
            if (Pod.ForgeFemtoclustersMissing > 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            pawn.skills?.Learn(SkillDefOf.Medicine, 0.08f);
            if (Pod.WorkGeneForge(1f))
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithProgressBar(PodInd, () => Pod.ForgeFraction);
        work.activeSkill = () => SkillDefOf.Medicine;
        work.handlingFacing = true;
        yield return work;
    }
}

public class WorkGiver_GeneForge : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building_TenebralPod pod in MapComponent_WorkBuildings.Of(pawn.Map).pods)
        {
            if (pod.Faction == Faction.OfPlayer && pod.GeneForgeWantsWork)
            {
                yield return pod;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !ModsConfig.BiotechActive || !PotentialWorkThingsGlobal(pawn).Any();

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Building_TenebralPod pod || !pod.GeneForgeWantsWork || t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        Job job = JobMaker.MakeJob(BOR_TenebralDefOf.BOR_GeneForge, pod);
        int missing = pod.ForgeFemtoclustersMissing;
        if (missing <= 0)
        {
            return job;
        }
        Thing? femto = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(BOR_TenebralDefOf.BOR_Femtoclusters),
            PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced));
        if (femto == null)
        {
            JobFailReason.Is("BOR_GeneForgeNeedsFemto".Translate(missing));
            return null;
        }
        job.targetB = femto;
        job.count = Mathf.Min(missing, femto.stackCount);
        return job;
    }
}

public class ITab_GeneForge : BorTab
{
    private const int Columns = 7;
    private const float CellSize = 52f;
    private const float CellSpacing = 6f;
    private const float Margin = 12f;
    private const float HeaderHeight = 30f;
    private const float FooterHeight = 34f;

    private static readonly Color CellBack = new(0.08f, 0.06f, 0.1f, 1f);
    private static readonly Color CellBorder = new(0.35f, 0.2f, 0.4f, 1f);
    private static readonly Color ProgressTint = new(0.55f, 0.1f, 0.1f, 0.55f);

    private static BorUISchemeDef? tenebralScheme;

    private readonly List<GeneDef> genes = new();

    public ITab_GeneForge()
    {
        labelKey = "BOR_TabGeneForge";
        size = new Vector2(440f, 300f);
    }

    private Building_TenebralPod? Pod => SelThing as Building_TenebralPod;

    public override bool IsVisible => Pod?.GeneForgeActive == true;

    // The pod has no scheme of its own (that would recolour the vanilla contents tab too), so the tab borrows the altar's.
    protected override BorUISchemeDef? Scheme =>
        base.Scheme ?? (tenebralScheme ??= DefDatabase<BorUISchemeDef>.GetNamedSilentFail("BOR_UIScheme_Tenebral"));

    protected override void UpdateSize()
    {
        Building_TenebralPod? pod = Pod;
        if (pod != null)
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(pod.ForgeableGenes().Count() / (float)Columns));
            size = new Vector2(Columns * CellSize + (Columns - 1) * CellSpacing + Margin * 2f,
                rows * CellSize + (rows - 1) * CellSpacing + Margin * 2f + HeaderHeight + FooterHeight + TitleHeight);
        }
        base.UpdateSize();
    }

    protected override void Contents(Rect rect)
    {
        Building_TenebralPod? pod = Pod;
        if (pod == null)
        {
            return;
        }
        genes.Clear();
        genes.AddRange(pod.ForgeableGenes());
        Rect inner = rect.ContractedBy(Margin);
        BorSkin skin = BorSkin.Current;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, HeaderHeight - 4f),
            "BOR_GeneForgeHeader".Translate(pod.ForgeFemtoclusters, Building_TenebralPod.ForgeFemtoclustersNeeded));
        Text.Anchor = TextAnchor.UpperLeft;

        if (genes.Count == 0)
        {
            GUI.color = skin.TextDim;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(inner.x, inner.y + HeaderHeight, inner.width, CellSize), "BOR_GeneForgeNoGenes".Translate(pod.Occupant!.LabelShortCap));
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            return;
        }

        for (int i = 0; i < genes.Count; i++)
        {
            Rect cell = new(inner.x + i % Columns * (CellSize + CellSpacing), inner.y + HeaderHeight + i / Columns * (CellSize + CellSpacing), CellSize, CellSize);
            DrawCell(cell, pod, genes[i], i);
        }

        DrawFooter(new Rect(inner.x, inner.yMax - FooterHeight + 6f, inner.width, FooterHeight - 6f), pod);
    }

    private static void DrawCell(Rect cell, Building_TenebralPod pod, GeneDef gene, int index)
    {
        BorSkin skin = BorSkin.Current;
        bool chosen = pod.ForgeGene == gene;
        skin.Cell(cell, CellBack, CellBorder);
        GUI.color = gene.IconColor;
        GUI.DrawTexture(cell.ContractedBy(6f), gene.Icon);
        GUI.color = Color.white;
        if (chosen && pod.ForgeFraction > 0f)
        {
            float h = cell.height * Mathf.Clamp01(pod.ForgeFraction);
            skin.Overlay(new Rect(cell.x, cell.yMax - h, cell.width, h), ProgressTint, BorTone.Bad);
        }
        if (chosen)
        {
            skin.Box(cell, Color.yellow);
        }
        else if (!skin.IsThemed)
        {
            skin.Box(cell, CellBorder);
        }
        skin.Hover(cell);
        TooltipHandler.TipRegion(cell, () => "BOR_GeneForgeCellTip".Translate(gene.LabelCap, gene.DescriptionFull,
            Building_TenebralPod.ForgeWorkTicks(gene).ToStringTicksToPeriod(), Building_TenebralPod.ForgeFemtoclustersNeeded,
            BOR_TenebralDefOf.BOR_Femtoclusters.label).Resolve(), 7394000 + index);
        if (Widgets.ButtonInvisible(cell))
        {
            pod.SelectForgeGene(chosen ? null : gene);
            (chosen ? SoundDefOf.Tick_Low : SoundDefOf.Tick_High).PlayOneShotOnCamera();
        }
    }

    private static void DrawFooter(Rect rect, Building_TenebralPod pod)
    {
        BorSkin skin = BorSkin.Current;
        GeneDef? gene = pod.ForgeGene;
        if (gene == null)
        {
            GUI.color = skin.TextDim;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(rect, "BOR_GeneForgeIdle".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            return;
        }
        Rect cancel = new(rect.xMax - rect.height, rect.y, rect.height, rect.height);
        Rect bar = new(rect.x, rect.y, rect.width - rect.height - 4f, rect.height);
        string label = pod.ForgeFemtoclustersMissing > 0
            ? "BOR_GeneForgeWaiting".Translate(gene.LabelCap, BOR_TenebralDefOf.BOR_Femtoclusters.label)
            : "BOR_GeneForgeProgress".Translate(gene.LabelCap);
        skin.Bar(bar, pod.ForgeFraction, label, BorTone.Bad);
        if (skin.Button(cancel, "X", tone: BorTone.Bad))
        {
            pod.SelectForgeGene(null);
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
        }
        TooltipHandler.TipRegion(cancel, "BOR_GeneForgeCancel".Translate());
    }
}
