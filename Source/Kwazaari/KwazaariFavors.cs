using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public static class KwazaariUtility
{
    private const int StacksPerPod = 8;

    private static StatDef? royalFavorValue;
    private static EffecterDef? skipEntry;

    private static StatDef? RoyalFavorValue => royalFavorValue ??= DefDatabase<StatDef>.GetNamedSilentFail("RoyalFavorValue");

    private static EffecterDef? SkipEntry => skipEntry ??= DefDatabase<EffecterDef>.GetNamedSilentFail("Skip_Entry");

    // Ultra-and-beyond items lying in the home area (on the ground or in storage), minus the blacklist.
    public static bool Sellable(Thing t) =>
        t.Spawned && t.def.category == ThingCategory.Item && t.def.techLevel >= TechLevel.Ultra
        && t is not MinifiedThing && t is not Corpse && t.Map.areaManager.Home[t.Position]
        && !KwazaariTradeBlacklistDef.Blocks(t.def);

    public static List<Thing> SellableThings(Map map) =>
        map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver).Where(Sellable).ToList();

    // Scrip for one unit: its royal favor value when it has one, else 1 plus 1 per 1000 silver of market value.
    public static float UnitScrip(Thing t)
    {
        float favor = RoyalFavorValue != null ? t.GetStatValue(RoyalFavorValue) : 0f;
        return favor > 0f ? favor : 1f + Mathf.Floor(t.MarketValue / 1000f);
    }

    // Sells `count` units of `t`: plays the skip effect where it lies and removes it. Returns the scrip earned.
    public static float SellAway(Thing t, int count)
    {
        count = Mathf.Min(count, t.stackCount);
        if (count <= 0 || !t.Spawned)
        {
            return 0f;
        }
        float earned = UnitScrip(t) * count;
        Map map = t.Map;
        IntVec3 cell = t.Position;
        if (SkipEntry != null)
        {
            Effecter effecter = SkipEntry.Spawn(cell, map);
            effecter.Trigger(new TargetInfo(cell, map), new TargetInfo(cell, map));
            map.effecterMaintainer.AddEffecterToMaintain(effecter, cell, SkipEntry.maintainTicks);
        }
        t.SplitOff(count).Destroy();
        return earned;
    }

    public static List<Thing> MakeStacks(ThingDef def, int count)
    {
        List<Thing> stacks = new();
        while (count > 0)
        {
            Thing t = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
            t.stackCount = Mathf.Min(count, def.stackLimit);
            count -= t.stackCount;
            stacks.Add(t);
        }
        return stacks;
    }

    // Drops the things in blue Kwazaari pods (the faction's dropPodIncoming) at the map's trade drop spot.
    public static void Deliver(Map map, List<Thing> things)
    {
        if (things.Count == 0)
        {
            return;
        }
        IntVec3 spot = DropCellFinder.TradeDropSpot(map);
        List<List<Thing>> groups = new();
        for (int i = 0; i < things.Count; i += StacksPerPod)
        {
            groups.Add(things.Skip(i).Take(StacksPerPod).ToList());
        }
        DropPodUtility.DropThingGroupsNear(spot, map, groups, forbid: false, faction: GameComponent_Kwazaari.Faction);
        Messages.Message("BOR_Kwazaari_Delivered".Translate(), new LookTargets(spot, map), MessageTypeDefOf.PositiveEvent);
    }
}

// What a favor does. Begin may ask the player to choose first; the cost is paid only once the delivery starts.
// `console` is the comm console the favor was requested from.
public abstract class FavorWorker
{
    public KwazaariFavorDef def = null!;

    public virtual AcceptanceReport CanDeliver(Map map) => true;

    public abstract void Begin(Map map, Thing console);

    protected bool TryPay(Map map)
    {
        AcceptanceReport report = GameComponent_Kwazaari.Instance.CanBuy(def, map);
        if (!report.Accepted || !GameComponent_Kwazaari.Instance.TryPay(def, map))
        {
            Messages.Message(report.Reason ?? "", MessageTypeDefOf.RejectInput, false);
            return false;
        }
        return true;
    }

    protected List<Thing> FixedThings() => def.things.SelectMany(tc => KwazaariUtility.MakeStacks(tc.thingDef, tc.count)).ToList();
}

// Drops the def's fixed list of things.
public class FavorWorker_Drop : FavorWorker
{
    public override void Begin(Map map, Thing console)
    {
        if (TryPay(map))
        {
            KwazaariUtility.Deliver(map, FixedThings());
        }
    }
}

// Player picks one thing from a list; chosenCount of it is dropped together with the fixed things.
public abstract class FavorWorker_Choice : FavorWorker
{
    protected abstract IEnumerable<ThingDef> Options();

    protected virtual string OptionLabel(ThingDef option) => option.LabelCap;

    public override AcceptanceReport CanDeliver(Map map) => Options().Any() ? true : "BOR_Kwazaari_NothingToChoose".Translate();

    public override void Begin(Map map, Thing console)
    {
        List<FloatMenuOption> options = Options().Select(option => new FloatMenuOption(OptionLabel(option), () =>
        {
            if (TryPay(map))
            {
                List<Thing> things = KwazaariUtility.MakeStacks(option, def.chosenCount);
                things.AddRange(FixedThings());
                KwazaariUtility.Deliver(map, things);
            }
        }, option)).ToList();
        Find.WindowStack.Add(new FloatMenu(options));
    }
}

public class FavorWorker_SkillTrainers : FavorWorker_Choice
{
    protected override IEnumerable<ThingDef> Options() =>
        DefDatabase<ThingDef>.AllDefs.Where(d => d.GetCompProperties<CompProperties_UseEffect_LearnSkill>()?.skill != null)
            .OrderBy(d => d.GetCompProperties<CompProperties_UseEffect_LearnSkill>().skill.listOrder);

    protected override string OptionLabel(ThingDef option) =>
        option.GetCompProperties<CompProperties_UseEffect_LearnSkill>().skill.LabelCap;
}

public class FavorWorker_StoneBlocks : FavorWorker_Choice
{
    protected override IEnumerable<ThingDef> Options() => ThingCategoryDefOf.StoneBlocks.childThingDefs;
}

// Orbital bombardment on a cell the player picks with a targeter.
public class FavorWorker_Bombardment : FavorWorker
{
    private const int StrikeDurationTicks = 540;

    public override void Begin(Map map, Thing console)
    {
        Find.Targeter.BeginTargeting(TargetingParameters.ForCell(), target =>
        {
            if (Find.CurrentMap != map || !TryPay(map))
            {
                return;
            }
            Bombardment strike = (Bombardment)GenSpawn.Spawn(ThingDefOf.Bombardment, target.Cell, map);
            strike.duration = StrikeDurationTicks;
            // Explosions read instigator.def; the console also makes the player the one blamed for damage.
            strike.instigator = console;
            strike.StartStrike();
        }, highlightAction: null, targetValidator: target => target.Cell.InBounds(map) && !target.Cell.Fogged(map));
    }
}

// Kwazaari soldiers lent for a while; see Building_KwazaariShuttle for arrival and pickup.
public class FavorWorker_Reinforcements : FavorWorker
{
    public override AcceptanceReport CanDeliver(Map map)
    {
        if (def.pawnKind == null || def.pawnCount <= 0)
        {
            return false;
        }
        if (GameComponent_Kwazaari.Instance.AnyLoanActive)
        {
            return "BOR_Kwazaari_LoanActive".Translate();
        }
        return Building_KwazaariShuttle.TryFindLandingSpot(map, out _) ? true : "BOR_Kwazaari_NoLandingSpot".Translate();
    }

    public override void Begin(Map map, Thing console)
    {
        if (!Building_KwazaariShuttle.TryFindLandingSpot(map, out IntVec3 cell) || !TryPay(map))
        {
            return;
        }
        Faction? faction = GameComponent_Kwazaari.Faction;
        KwazaariLoan loan = new KwazaariLoan
        {
            map = map,
            returnTick = Find.TickManager.TicksGame + Mathf.RoundToInt(def.loanHours * GenDate.TicksPerHour),
            lostPawnCost = def.lostPawnCost
        };
        for (int i = 0; i < def.pawnCount; i++)
        {
            loan.pawns.Add(PawnGenerator.GeneratePawn(def.pawnKind, faction));
        }
        GameComponent_Kwazaari.Instance.AddLoan(loan);
        Building_KwazaariShuttle.SendDelivery(loan, cell);
    }
}
