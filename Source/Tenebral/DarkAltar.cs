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

// A special forge: every cell is prefilled with a tenebral item but locked until worked on.
// Unlocking is shared by every altar (GameComponent_DarkAltar); production progress stays on the altar.
public class CompProperties_DarkAltar : CompProperties
{
    public int gridWidth = 4;
    public List<DarkAltarEntry> entries = new();

    // Unlock work = forge analysis ticks for the item x this.
    public float unlockWorkFactor = 1f;

    // Production work = forge production ticks for the item x this.
    public float produceWorkFactor = 1f;

    // Crystalline joints per run = item value x yield x this / joint value (at least 1).
    public float componentCostFactor = 0.5f;

    // Bloodlust drained per full day of work at the altar.
    public float bloodlustDrainPerDay = 2f;

    public CompProperties_DarkAltar()
    {
        compClass = typeof(CompDarkAltar);
    }
}

public sealed class CompDarkAltar : ThingComp
{
    private const float BaseTicksFactor = 2f;
    private const int CountRefreshTicks = 60;

    // The altar offers the forge's counted modes, but not "forever".
    public static readonly ForgeMode[] Modes = { ForgeMode.DoX, ForgeMode.DoUntil };

    // Worked out once per cell, as the produce job asks on every work tick.
    private int[]? componentCosts;

    // Refreshed about once a second, as "until" cells ask on every work scan and the tab on every frame. -1 = not counted yet.
    private int[]? countsOnMap;
    private int countsTick = -CountRefreshTicks;

    private List<int> modes = new();
    private List<int> queued = new();
    private List<float> produceProgress = new();
    private int components;

    // Unsealing from saves made before it was shared, held from loading until the whole game is loaded.
    private List<bool>? oldUnlocked;
    private List<bool>? oldRequested;
    private List<float>? oldProgress;

    public CompProperties_DarkAltar Props => (CompProperties_DarkAltar)props;

    public int Count => Props.entries.Count;

    public int StoredComponents => components;

    private static GameComponent_DarkAltar Seals => GameComponent_DarkAltar.Instance;

    private void EnsureSize()
    {
        int n = Count;
        while (queued.Count < n)
        {
            queued.Add(0);
        }
        while (produceProgress.Count < n)
        {
            produceProgress.Add(0f);
        }
        while (modes.Count < n)
        {
            modes.Add((int)ForgeMode.DoX);
        }
    }

    public ForgeMode ModeOf(int i)
    {
        EnsureSize();
        return (ForgeMode)modes[i];
    }

    public void SetMode(int i, ForgeMode mode)
    {
        EnsureSize();
        modes[i] = (int)mode;
    }

    public int CountOnMap(int i)
    {
        Map? map = parent.Map;
        if (map == null)
        {
            return 0;
        }
        int now = Find.TickManager.TicksGame;
        if (countsOnMap == null || countsOnMap.Length != Count)
        {
            countsOnMap = new int[Count];
            countsTick = -CountRefreshTicks;
        }
        if (now - countsTick >= CountRefreshTicks)
        {
            for (int k = 0; k < countsOnMap.Length; k++)
            {
                countsOnMap[k] = -1;
            }
            countsTick = now;
        }
        if (countsOnMap[i] < 0)
        {
            int total = 0;
            foreach (Thing t in map.listerThings.ThingsOfDef(Entry(i).thing))
            {
                total += t.stackCount;
            }
            countsOnMap[i] = total;
        }
        return countsOnMap[i];
    }

    public bool ShouldProduce(int i)
    {
        EnsureSize();
        if (!IsUnlocked(i) || queued[i] <= 0)
        {
            return false;
        }
        return (ForgeMode)modes[i] != ForgeMode.DoUntil || CountOnMap(i) < queued[i];
    }

    public DarkAltarEntry Entry(int i) => Props.entries[i];

    // Unsealing is shared by every altar: these ask GameComponent_DarkAltar.
    public bool IsUnlocked(int i) => Seals.IsUnsealed(Entry(i).thing);

    public bool UnlockRequested(int i) => Seals.IsRequested(Entry(i).thing);

    public void ToggleUnlock(int i) => Seals.ToggleRequest(Entry(i).thing);

    public int Queued(int i)
    {
        EnsureSize();
        return queued[i];
    }

    public void AddQueued(int i, int amount)
    {
        EnsureSize();
        if (IsUnlocked(i))
        {
            queued[i] = Mathf.Max(0, queued[i] + amount);
            if (queued[i] == 0)
            {
                produceProgress[i] = 0f;
            }
        }
    }

    public int UnlockTicks(int i) => Mathf.Max(60, Mathf.RoundToInt(ForgeMath.BaseWorkTicks(Entry(i).thing) * BaseTicksFactor * Props.unlockWorkFactor));

    public int ProduceTicks(int i) => Mathf.Max(30, Mathf.RoundToInt(ForgeMath.BaseWorkTicks(Entry(i).thing) * Props.produceWorkFactor));

    public float UnlockFraction(int i) => Seals.Progress(Entry(i).thing) / UnlockTicks(i);

    public float ProduceFraction(int i)
    {
        EnsureSize();
        return produceProgress[i] / ProduceTicks(i);
    }

    public int ComponentCost(int i)
    {
        componentCosts ??= new int[Count];
        if (componentCosts[i] == 0)
        {
            ThingDef def = Entry(i).thing;
            float value = def.GetStatValueAbstract(StatDefOf.MarketValue, Entry(i).stuff) * ForgeMath.YieldFor(def);
            componentCosts[i] = Mathf.Max(1, Mathf.CeilToInt(value * Props.componentCostFactor / ForgeMath.MarketValue(BOR_TenebralDefOf.BOR_ComponentIII)));
        }
        return componentCosts[i];
    }

    public int NextUnlockCell()
    {
        for (int i = 0; i < Count; i++)
        {
            if (!IsUnlocked(i) && UnlockRequested(i))
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
            if (ShouldProduce(i))
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

    // Returns true when the cell finished unlocking on this tick, for every altar.
    public bool WorkUnlock(int i)
    {
        if (!Seals.AddProgress(Entry(i).thing, 1f, UnlockTicks(i)))
        {
            return false;
        }
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
        // "Until" keeps its target; the count on the map stops it.
        if (modes[i] == (int)ForgeMode.DoX)
        {
            queued[i] = Mathf.Max(0, queued[i] - 1);
        }
        DarkAltarEntry entry = Entry(i);
        ThingDef? stuff = entry.thing.MadeFromStuff ? entry.stuff ?? GenStuff.DefaultStuffFor(entry.thing) : null;
        Thing product = ThingMaker.MakeThing(entry.thing, stuff);
        product.stackCount = ForgeMath.YieldFor(entry.thing);
        product.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        GenPlace.TryPlaceThing(product, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
        countsTick = -CountRefreshTicks;
        return true;
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        MapComponent_WorkBuildings.Of(parent.Map).altars.Add(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_WorkBuildings.Of(map).altars.Remove(this);
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

    // Instant unlock / relock for testing, for every altar.
    public void DevSetUnlocked(int i, bool value)
    {
        EnsureSize();
        Seals.SetUnsealed(Entry(i).thing, value);
        if (!value)
        {
            queued[i] = 0;
            produceProgress[i] = 0f;
        }
    }

    // Burns what the altar holds, so nothing drops when the Path of Light strikes it.
    public void ClearComponents() => components = 0;

    public void DevAddComponents(int amount) => components = Mathf.Max(0, components + amount);

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!DebugSettings.godMode)
        {
            yield break;
        }
        yield return DevCommand("DEV: unlock all", () => { for (int i = 0; i < Count; i++) { DevSetUnlocked(i, true); } });
        yield return DevCommand("DEV: lock all", () => { for (int i = 0; i < Count; i++) { DevSetUnlocked(i, false); } });
        yield return DevCommand("DEV: +10 joints", () => DevAddComponents(10));
        yield return DevCommand("DEV: -10 joints", () => DevAddComponents(-10));
        yield return DevCommand("DEV: clear joints", () => components = 0);
    }

    private static Command_Action DevCommand(string label, System.Action action) => new()
    {
        defaultLabel = label,
        defaultDesc = "God mode only. Ctrl+click a cell in the altar tab to unlock or lock just that one.",
        action = action
    };

    public override string CompInspectStringExtra() => "BOR_AltarComponents".Translate(components);

    // Right-clicking the altar explains why a colonist cannot work it.
    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (selPawn.RaceProps.Humanlike && !TenebralPsychosis.IsPsychotic(selPawn))
        {
            yield return new FloatMenuOption("BOR_AltarNeedsPsychosis".Translate(
                TenebralPsychosis.Severity(selPawn).ToStringPercent(), TenebralPsychosis.PsychoticSeverity.ToStringPercent()), null);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref queued, "altarQueued", LookMode.Value);
        Scribe_Collections.Look(ref produceProgress, "altarProduceProgress", LookMode.Value);
        Scribe_Values.Look(ref components, "altarComponents");
        Scribe_Collections.Look(ref modes, "altarModes", LookMode.Value);
        modes ??= new List<int>();
        queued ??= new List<int>();
        produceProgress ??= new List<float>();

        // Older saves kept unsealing on each altar. Read it while loading, and hand it over once the whole game is loaded.
        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Collections.Look(ref oldUnlocked, "altarUnlocked", LookMode.Value);
            Scribe_Collections.Look(ref oldRequested, "altarUnlockRequested", LookMode.Value);
            Scribe_Collections.Look(ref oldProgress, "altarUnlockProgress", LookMode.Value);
        }
        else if (Scribe.mode == LoadSaveMode.PostLoadInit && (oldUnlocked != null || oldRequested != null || oldProgress != null))
        {
            for (int i = 0; i < Count; i++)
            {
                Seals.CarryOver(Entry(i).thing,
                    oldUnlocked != null && i < oldUnlocked.Count && oldUnlocked[i],
                    oldRequested != null && i < oldRequested.Count && oldRequested[i],
                    oldProgress != null && i < oldProgress.Count ? oldProgress[i] : 0f);
            }
            oldUnlocked = null;
            oldRequested = null;
            oldProgress = null;
        }
    }
}

// Which altar designs are unsealed, which ones colonists are asked to unseal, and how far each has got.
// Shared by every dark archotech altar on every map, so each design is unsealed once for all of them.
// Keyed by defName, so a design from a mod that was since removed still loads without errors.
public class GameComponent_DarkAltar : GameComponent
{
    private HashSet<string> unsealed = new();
    private HashSet<string> requested = new();
    private Dictionary<string, float> progress = new();

    public GameComponent_DarkAltar(Game game)
    {
    }

    public static GameComponent_DarkAltar Instance => Current.Game.GetComponent<GameComponent_DarkAltar>();

    public bool IsUnsealed(ThingDef design) => unsealed.Contains(design.defName);

    public bool IsRequested(ThingDef design) => requested.Contains(design.defName);

    public float Progress(ThingDef design) => progress.TryGetValue(design.defName, out float done) ? done : 0f;

    public void ToggleRequest(ThingDef design)
    {
        if (!IsUnsealed(design) && !requested.Remove(design.defName))
        {
            requested.Add(design.defName);
        }
    }

    // Returns true when this work finished unsealing the design.
    public bool AddProgress(ThingDef design, float amount, float needed)
    {
        float done = Progress(design) + amount;
        if (done < needed)
        {
            progress[design.defName] = done;
            return false;
        }
        SetUnsealed(design, true);
        return true;
    }

    public void SetUnsealed(ThingDef design, bool value)
    {
        requested.Remove(design.defName);
        progress.Remove(design.defName);
        if (value)
        {
            unsealed.Add(design.defName);
        }
        else
        {
            unsealed.Remove(design.defName);
        }
    }

    // From a save that kept unsealing on each altar: what any altar unsealed or was asked to unseal carries over, with the furthest progress.
    public void CarryOver(ThingDef design, bool wasUnsealed, bool wasRequested, float wasProgress)
    {
        if (IsUnsealed(design))
        {
            return;
        }
        if (wasUnsealed)
        {
            SetUnsealed(design, true);
            return;
        }
        if (wasRequested)
        {
            requested.Add(design.defName);
        }
        if (wasProgress > Progress(design))
        {
            progress[design.defName] = wasProgress;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref unsealed, "unsealed", LookMode.Value);
        Scribe_Collections.Look(ref requested, "requested", LookMode.Value);
        Scribe_Collections.Look(ref progress, "progress", LookMode.Value, LookMode.Value);
        unsealed ??= new HashSet<string>();
        requested ??= new HashSet<string>();
        progress ??= new Dictionary<string, float>();
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

    protected override bool StillWanted(CompDarkAltar comp) => Cell >= 0 && comp.ShouldProduce(Cell);

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
        foreach (CompDarkAltar comp in MapComponent_WorkBuildings.Of(pawn.Map).altars)
        {
            if (comp.parent.Faction == Faction.OfPlayer && HasWork(comp))
            {
                yield return comp.parent;
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

public class ITab_DarkAltar : BorTab
{
    private const float CellSize = 64f;
    private const float CellSpacing = 6f;
    private const float Margin = 12f;
    private const float HeaderHeight = 30f;
    private const float ModeBarHeight = 14f;

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
                h * CellSize + (h - 1) * CellSpacing + Margin * 2f + HeaderHeight + TitleHeight);
        }
        base.UpdateSize();
    }

    protected override void Contents(Rect rect)
    {
        CompDarkAltar? altar = Altar;
        if (altar == null)
        {
            return;
        }
        Rect inner = rect.ContractedBy(Margin);
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
        BorSkin skin = BorSkin.Current;
        skin.Cell(cell, CellBack, CellBorder);
        Rect modeBar = new(cell.x + 2f, cell.yMax - ModeBarHeight - 2f, cell.width - 4f, ModeBarHeight);
        Widgets.DefIcon(open ? new Rect(cell.x + 8f, cell.y + 6f, cell.width - 16f, cell.height - 12f - ModeBarHeight) : cell.ContractedBy(8f), entry.thing, entry.stuff);
        float fraction = open ? (altar.Queued(i) > 0 ? altar.ProduceFraction(i) : 0f) : altar.UnlockFraction(i);
        if (!open)
        {
            skin.Fill(cell, LockedTint);
        }
        if (fraction > 0f)
        {
            float h = cell.height * Mathf.Clamp01(fraction);
            skin.Overlay(new Rect(cell.x, cell.yMax - h, cell.width, h), ProgressTint, BorTone.Bad);
        }
        if (!open && altar.UnlockRequested(i))
        {
            skin.Box(cell, Color.yellow);
        }
        else if (!skin.IsThemed)
        {
            skin.Box(cell, CellBorder); // vanilla redraws the border over the overlays; the themed cell keeps its own
        }

        Text.Font = GameFont.Tiny;
        if (open)
        {
            if (altar.Queued(i) > 0)
            {
                Text.Anchor = TextAnchor.UpperRight;
                Widgets.Label(new Rect(cell.x, cell.y + 2f, cell.width - 4f, 20f), ITab_Forge.ShortCount(altar.Queued(i)));
            }
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(new Rect(cell.x + 4f, modeBar.y - 20f, cell.width - 4f, 20f), ITab_Forge.ShortCount(altar.CountOnMap(i)));
        }
        else
        {
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(new Rect(cell.x, cell.y, cell.width, cell.height - 2f), "BOR_AltarLocked".Translate());
        }
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;

        if (open)
        {
            ITab_Forge.DrawModeBar(modeBar, altar.ModeOf(i), CompDarkAltar.Modes, mode => altar.SetMode(i, mode));
        }

        skin.Hover(cell);
        TooltipHandler.TipRegion(cell, () => open
            ? "BOR_AltarCellTip".Translate(entry.thing.LabelCap, ForgeMath.YieldFor(entry.thing), altar.ComponentCost(i)).Resolve()
            : "BOR_AltarLockedTip".Translate(entry.thing.LabelCap, altar.UnlockTicks(i).ToStringTicksToPeriod(), ForgeMath.YieldFor(entry.thing), altar.ComponentCost(i)).Resolve(), 7393000 + i);

        // The mode bar handles its own clicks.
        if (open && Mouse.IsOver(modeBar))
        {
            return;
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && Mouse.IsOver(cell) && open)
        {
            altar.AddQueued(i, Event.current.shift ? -5 : -1);
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            Event.current.Use();
            return;
        }
        if (Widgets.ButtonInvisible(cell))
        {
            if (DebugSettings.godMode && Event.current.control)
            {
                altar.DevSetUnlocked(i, !open);
            }
            else if (open)
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
