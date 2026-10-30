using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BeyondOurReach;

public class DarkAltarEntry
{
    public ThingDef thing = null!;
    public ThingDef? stuff;
}

// A special forge: every cell is prefilled with a tenebral item but locked until worked on. Progress stays on the altar.
public class CompProperties_DarkAltar : CompProperties
{
    public int gridWidth = 4;
    public List<DarkAltarEntry> entries = new();

    // Unlock work = forge analysis ticks for the item x this.
    public float unlockWorkFactor = 1f;

    // Crystalline joints per run = item value x yield x this / joint value (at least 1).
    public float componentCostFactor = 0.5f;

    // Bloodlust drained per full day of work at the altar.
    public float bloodlustDrainPerDay = 2f;

    public CompProperties_DarkAltar()
    {
        compClass = typeof(CompDarkAltar);
    }
}

public class CompDarkAltar : ThingComp
{
    private const float BaseTicksFactor = 2f;

    private List<bool> unlocked = new();
    private List<bool> unlockRequested = new();
    private List<float> unlockProgress = new();
    private List<int> queued = new();
    private List<float> produceProgress = new();
    private int components;

    public CompProperties_DarkAltar Props => (CompProperties_DarkAltar)props;

    public int Count => Props.entries.Count;

    public int StoredComponents => components;

    private void EnsureSize()
    {
        int n = Count;
        while (unlocked.Count < n)
        {
            unlocked.Add(false);
        }
        while (unlockRequested.Count < n)
        {
            unlockRequested.Add(false);
        }
        while (unlockProgress.Count < n)
        {
            unlockProgress.Add(0f);
        }
        while (queued.Count < n)
        {
            queued.Add(0);
        }
        while (produceProgress.Count < n)
        {
            produceProgress.Add(0f);
        }
    }

    public DarkAltarEntry Entry(int i) => Props.entries[i];

    public bool IsUnlocked(int i)
    {
        EnsureSize();
        return unlocked[i];
    }

    public bool UnlockRequested(int i)
    {
        EnsureSize();
        return unlockRequested[i];
    }

    public void ToggleUnlock(int i)
    {
        EnsureSize();
        if (!unlocked[i])
        {
            unlockRequested[i] = !unlockRequested[i];
        }
    }

    public int Queued(int i)
    {
        EnsureSize();
        return queued[i];
    }

    public void AddQueued(int i, int amount)
    {
        EnsureSize();
        if (unlocked[i])
        {
            queued[i] = Mathf.Max(0, queued[i] + amount);
            if (queued[i] == 0)
            {
                produceProgress[i] = 0f;
            }
        }
    }

    public int UnlockTicks(int i) => Mathf.Max(60, Mathf.RoundToInt(ForgeMath.BaseWorkTicks(Entry(i).thing) * BaseTicksFactor * Props.unlockWorkFactor));

    public int ProduceTicks(int i) => Mathf.Max(30, Mathf.RoundToInt(ForgeMath.BaseWorkTicks(Entry(i).thing)));

    public float UnlockFraction(int i)
    {
        EnsureSize();
        return unlockProgress[i] / UnlockTicks(i);
    }

    public float ProduceFraction(int i)
    {
        EnsureSize();
        return produceProgress[i] / ProduceTicks(i);
    }

    public int ComponentCost(int i)
    {
        ThingDef def = Entry(i).thing;
        float value = def.GetStatValueAbstract(StatDefOf.MarketValue, Entry(i).stuff) * ForgeMath.YieldFor(def);
        return Mathf.Max(1, Mathf.CeilToInt(value * Props.componentCostFactor / BOR_TenebralDefOf.BOR_ComponentIII.BaseMarketValue));
    }

    public int NextUnlockCell()
    {
        EnsureSize();
        for (int i = 0; i < Count; i++)
        {
            if (!unlocked[i] && unlockRequested[i])
            {
                return i;
            }
        }
        return -1;
    }

    public int NextProduceCell()
    {
        EnsureSize();
        for (int i = 0; i < Count; i++)
        {
            if (unlocked[i] && queued[i] > 0)
            {
                return i;
            }
        }
        return -1;
    }

    public int ComponentsMissing(int i) => Mathf.Max(0, ComponentCost(i) - components);

    public void DepositComponents(Thing thing)
    {
        components += thing.stackCount;
        thing.Destroy();
    }

    public static void DrainBloodlust(Pawn pawn, float perDay)
    {
        Need_Murder? need = pawn.needs?.TryGetNeed<Need_Murder>();
        if (need != null)
        {
            need.CurLevel -= perDay / GenDate.TicksPerDay;
        }
    }

    // Returns true when the cell finished unlocking on this tick.
    public bool WorkUnlock(int i)
    {
        EnsureSize();
        unlockProgress[i] += 1f;
        if (unlockProgress[i] < UnlockTicks(i))
        {
            return false;
        }
        unlocked[i] = true;
        unlockRequested[i] = false;
        unlockProgress[i] = 0f;
        Messages.Message("BOR_AltarUnlocked".Translate(Entry(i).thing.LabelCap), parent, MessageTypeDefOf.PositiveEvent);
        return true;
    }

    // Returns true when a run finished on this tick.
    public bool WorkProduce(int i)
    {
        EnsureSize();
        produceProgress[i] += 1f;
        if (produceProgress[i] < ProduceTicks(i))
        {
            return false;
        }
        produceProgress[i] = 0f;
        components -= ComponentCost(i);
        queued[i] = Mathf.Max(0, queued[i] - 1);
        DarkAltarEntry entry = Entry(i);
        ThingDef? stuff = entry.thing.MadeFromStuff ? entry.stuff ?? GenStuff.DefaultStuffFor(entry.thing) : null;
        Thing product = ThingMaker.MakeThing(entry.thing, stuff);
        product.stackCount = ForgeMath.YieldFor(entry.thing);
        product.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        GenPlace.TryPlaceThing(product, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
        return true;
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (components > 0 && previousMap != null)
        {
            Thing joints = ThingMaker.MakeThing(BOR_TenebralDefOf.BOR_ComponentIII);
            joints.stackCount = components;
            GenPlace.TryPlaceThing(joints, parent.Position, previousMap, ThingPlaceMode.Near);
            components = 0;
        }
    }

    public override string CompInspectStringExtra() => "BOR_AltarComponents".Translate(components);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref unlocked, "altarUnlocked", LookMode.Value);
        Scribe_Collections.Look(ref unlockRequested, "altarUnlockRequested", LookMode.Value);
        Scribe_Collections.Look(ref unlockProgress, "altarUnlockProgress", LookMode.Value);
        Scribe_Collections.Look(ref queued, "altarQueued", LookMode.Value);
        Scribe_Collections.Look(ref produceProgress, "altarProduceProgress", LookMode.Value);
        Scribe_Values.Look(ref components, "altarComponents");
        unlocked ??= new List<bool>();
        unlockRequested ??= new List<bool>();
        unlockProgress ??= new List<float>();
        queued ??= new List<int>();
        produceProgress ??= new List<float>();
    }
}

public abstract class JobDriver_AltarWork : JobDriver
{
    protected const TargetIndex AltarInd = TargetIndex.A;
    protected const TargetIndex ComponentInd = TargetIndex.B;

    protected Thing AltarThing => job.GetTarget(AltarInd).Thing;
    protected CompDarkAltar? Comp => AltarThing?.TryGetComp<CompDarkAltar>();
    protected virtual int Cell => job.count;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(AltarThing, job, 1, -1, null, errorOnFailed))
        {
            return false;
        }
        return !job.GetTarget(ComponentInd).IsValid || pawn.Reserve(job.GetTarget(ComponentInd), job, 1, -1, null, errorOnFailed);
    }

    protected abstract bool StillWanted(CompDarkAltar comp);

    // Advances the cell by one tick; true when finished.
    protected abstract bool WorkTick(CompDarkAltar comp);

    protected abstract float Fraction(CompDarkAltar comp);

    protected virtual IEnumerable<Toil> PreWorkToils()
    {
        yield break;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(AltarInd);
        this.FailOn(() => Comp == null || !StillWanted(Comp));
        foreach (Toil toil in PreWorkToils())
        {
            yield return toil;
        }
        yield return Toils_Goto.GotoThing(AltarInd, PathEndMode.InteractionCell);
        Toil work = ToilMaker.MakeToil("AltarWork");
        // Psychosis is checked once, when the work starts, not every tick.
        work.initAction = () =>
        {
            if (!TenebralPsychosis.IsPsychotic(pawn))
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        work.tickAction = () =>
        {
            CompDarkAltar? comp = Comp;
            if (comp == null)
            {
                return;
            }
            pawn.rotationTracker.FaceTarget(AltarThing);
            CompDarkAltar.DrainBloodlust(pawn, comp.Props.bloodlustDrainPerDay);
            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.08f);
            if (WorkTick(comp))
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithProgressBar(AltarInd, () => Comp == null ? 0f : Fraction(Comp));
        work.activeSkill = () => SkillDefOf.Intellectual;
        work.handlingFacing = true;
        yield return work;
    }
}

public class JobDriver_AltarUnlock : JobDriver_AltarWork
{
    protected override bool StillWanted(CompDarkAltar comp) => !comp.IsUnlocked(Cell) && comp.UnlockRequested(Cell);

    protected override bool WorkTick(CompDarkAltar comp) => comp.WorkUnlock(Cell);

    protected override float Fraction(CompDarkAltar comp) => comp.UnlockFraction(Cell);
}

public class JobDriver_AltarProduce : JobDriver_AltarWork
{
    // job.count is the number of crystalline joints to carry, so the cell is picked when the job starts.
    private int cell = -1;

    protected override int Cell => cell;

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        if (cell < 0)
        {
            cell = Comp?.NextProduceCell() ?? -1;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cell, "altarCell", -1);
    }

    protected override bool StillWanted(CompDarkAltar comp) => Cell >= 0 && comp.Queued(Cell) > 0;

    protected override bool WorkTick(CompDarkAltar comp)
    {
        if (comp.ComponentsMissing(Cell) > 0)
        {
            EndJobWith(JobCondition.Incompletable);
            return false;
        }
        return comp.WorkProduce(Cell);
    }

    protected override float Fraction(CompDarkAltar comp) => comp.ProduceFraction(Cell);

    // Brings the crystalline joints the altar requests before a run.
    protected override IEnumerable<Toil> PreWorkToils()
    {
        if (!job.GetTarget(ComponentInd).IsValid)
        {
            yield break;
        }
        yield return Toils_Goto.GotoThing(ComponentInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(ComponentInd);
        yield return Toils_Haul.StartCarryThing(ComponentInd);
        yield return Toils_Goto.GotoThing(AltarInd, PathEndMode.InteractionCell);
        Toil deposit = ToilMaker.MakeToil("AltarDeposit");
        deposit.initAction = () =>
        {
            Thing? carried = pawn.carryTracker.CarriedThing;
            if (carried != null && Comp != null)
            {
                pawn.carryTracker.innerContainer.Remove(carried);
                Comp.DepositComponents(carried);
            }
        };
        deposit.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return deposit;
    }
}

public class WorkGiver_DarkAltar : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    private static bool HasWork(CompDarkAltar comp) => comp.NextUnlockCell() >= 0 || comp.NextProduceCell() >= 0;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Building b in pawn.Map.listerBuildings.allBuildingsColonist)
        {
            CompDarkAltar? comp = b.TryGetComp<CompDarkAltar>();
            if (comp != null && HasWork(comp))
            {
                yield return b;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        !TenebralPsychosis.IsPsychotic(pawn) || !PotentialWorkThingsGlobal(pawn).Any();

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        CompDarkAltar? comp = t.TryGetComp<CompDarkAltar>();
        if (comp == null || !TenebralPsychosis.IsPsychotic(pawn) || t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
        {
            return null;
        }
        int cell = comp.NextUnlockCell();
        if (cell >= 0)
        {
            Job unlock = JobMaker.MakeJob(BOR_TenebralDefOf.BOR_AltarUnlock, t);
            unlock.count = cell;
            return unlock;
        }
        cell = comp.NextProduceCell();
        if (cell < 0)
        {
            return null;
        }
        int missing = comp.ComponentsMissing(cell);
        Job produce = JobMaker.MakeJob(BOR_TenebralDefOf.BOR_AltarProduce, t);
        if (missing <= 0)
        {
            return produce;
        }
        Thing? joints = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(BOR_TenebralDefOf.BOR_ComponentIII),
            PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, -1, null, forced));
        if (joints == null)
        {
            JobFailReason.Is("BOR_AltarNeedsComponents".Translate(missing));
            return null;
        }
        produce.targetB = joints;
        produce.count = Mathf.Min(missing, joints.stackCount);
        return produce;
    }
}

public class ITab_DarkAltar : ITab
{
    private const float CellSize = 64f;
    private const float CellSpacing = 6f;
    private const float Margin = 12f;
    private const float HeaderHeight = 30f;

    private static readonly Color CellBack = new(0.12f, 0.06f, 0.06f, 1f);
    private static readonly Color CellBorder = new(0.45f, 0.15f, 0.15f, 1f);
    private static readonly Color LockedTint = new(0f, 0f, 0f, 0.6f);
    private static readonly Color ProgressTint = new(0.55f, 0.1f, 0.1f, 0.55f);

    public ITab_DarkAltar()
    {
        labelKey = "BOR_TabDarkAltar";
        size = new Vector2(320f, 320f);
    }

    private CompDarkAltar? Altar => SelThing?.TryGetComp<CompDarkAltar>();

    public override bool IsVisible => Altar != null;

    protected override void UpdateSize()
    {
        CompDarkAltar? altar = Altar;
        if (altar != null)
        {
            int w = altar.Props.gridWidth;
            int h = Mathf.CeilToInt(altar.Count / (float)w);
            size = new Vector2(Mathf.Max(260f, w * CellSize + (w - 1) * CellSpacing + Margin * 2f),
                h * CellSize + (h - 1) * CellSpacing + Margin * 2f + HeaderHeight);
        }
        base.UpdateSize();
    }

    protected override void FillTab()
    {
        CompDarkAltar? altar = Altar;
        if (altar == null)
        {
            return;
        }
        Rect inner = new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, HeaderHeight - 4f), "BOR_AltarHeader".Translate(altar.StoredComponents));
        Text.Anchor = TextAnchor.UpperLeft;
        int width = altar.Props.gridWidth;
        float gridPx = width * CellSize + (width - 1) * CellSpacing;
        float startX = inner.x + (inner.width - gridPx) / 2f;
        for (int i = 0; i < altar.Count; i++)
        {
            Rect cell = new(startX + i % width * (CellSize + CellSpacing), inner.y + HeaderHeight + i / width * (CellSize + CellSpacing), CellSize, CellSize);
            DrawCell(cell, altar, i);
        }
    }

    private static void DrawCell(Rect cell, CompDarkAltar altar, int i)
    {
        DarkAltarEntry entry = altar.Entry(i);
        bool open = altar.IsUnlocked(i);
        Widgets.DrawBoxSolid(cell, CellBack);
        Widgets.DefIcon(cell.ContractedBy(8f), entry.thing, entry.stuff);
        float fraction = open ? (altar.Queued(i) > 0 ? altar.ProduceFraction(i) : 0f) : altar.UnlockFraction(i);
        if (!open)
        {
            Widgets.DrawBoxSolid(cell, LockedTint);
        }
        if (fraction > 0f)
        {
            float h = cell.height * Mathf.Clamp01(fraction);
            Widgets.DrawBoxSolid(new Rect(cell.x, cell.yMax - h, cell.width, h), ProgressTint);
        }
        GUI.color = !open && altar.UnlockRequested(i) ? Color.yellow : CellBorder;
        Widgets.DrawBox(cell);
        GUI.color = Color.white;

        Text.Font = GameFont.Tiny;
        if (open && altar.Queued(i) > 0)
        {
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(cell.x, cell.y + 2f, cell.width - 4f, 20f), altar.Queued(i).ToString());
        }
        else if (!open)
        {
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(new Rect(cell.x, cell.y, cell.width, cell.height - 2f), "BOR_AltarLocked".Translate());
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        Widgets.DrawHighlightIfMouseover(cell);
        TooltipHandler.TipRegion(cell, () => open
            ? "BOR_AltarCellTip".Translate(entry.thing.LabelCap, ForgeMath.YieldFor(entry.thing), altar.ComponentCost(i)).Resolve()
            : "BOR_AltarLockedTip".Translate(entry.thing.LabelCap, altar.UnlockTicks(i).ToStringTicksToPeriod()).Resolve(), 7393000 + i);

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(cell) && open)
        {
            altar.AddQueued(i, Event.current.shift ? -5 : -1);
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            Event.current.Use();
            return;
        }
        if (Widgets.ButtonInvisible(cell))
        {
            if (open)
            {
                altar.AddQueued(i, Event.current.shift ? 5 : 1);
            }
            else
            {
                altar.ToggleUnlock(i);
            }
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }
    }
}
