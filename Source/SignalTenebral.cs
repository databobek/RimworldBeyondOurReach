using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace BeyondOurReach;

[DefOf]
public static class BOR_SignalTenebralDefOf
{
    public static ThingDef BOR_TenebralShuttle = null!;
    public static ThingDef BOR_TenebralShuttleIncoming = null!;
    public static ThingDef BOR_TenebralPodIncoming = null!;

    static BOR_SignalTenebralDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_SignalTenebralDefOf));
    }
}

public class CompProperties_SignalTenebral : CompProperties_SignalRaid
{
    public FactionDef? badzaariFaction;
    public PawnKindDef? visitorKind;
    // Dropped by the deep-red shuttle, which only comes when the Badzaari (and their motor channel) are missing.
    public ThingDef? shuttleReward;

    // Dialogue is a chain of dialoguePrefix + _Stage1.._StageN nodes. Every stage but the last offers "End conversation"
    // (closes at once) and dialoguePrefix + _Ask (next stage); the last one's single option, dialoguePrefix + _Final, brings the tenebral.
    public int dialogueStages = 4;

    public CompProperties_SignalTenebral()
    {
        compClass = typeof(CompSignalTenebral);
    }
}

// Signal channel answered by a lone tenebral carrying the reward. With the Badzaari around he drops in a deep-red pod;
// without them he comes in a deep-red shuttle that also yields the tenebrite motor, which the Badzaari channel would
// otherwise provide. If the tenebral faction does not exist yet, answering creates it by taking over a settlement.
public class CompSignalTenebral : CompSignalRaid
{
    private Pawn? visitor;

    private new CompProperties_SignalTenebral Props => (CompProperties_SignalTenebral)props;

    private bool BadzaariPresent => Props.badzaariFaction != null && Find.FactionManager.FirstFactionOfDef(Props.badzaariFaction) != null;

    protected override bool ChannelExists => true;

    protected override IEnumerable<ThingDef> Rewards
    {
        get
        {
            if (Props.reward != null)
            {
                yield return Props.reward;
            }
            if (Props.shuttleReward != null && !BadzaariPresent)
            {
                yield return Props.shuttleReward;
            }
        }
    }

    protected override bool Busy =>
        (visitor != null && !visitor.Dead && visitor.MapHeld == parent.Map)
        || (Props.shuttleReward != null && Raids.Active(Props.shuttleReward));

    protected override string BusyReason => "BOR_Tenebral_Busy".Translate();

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (DebugSettings.ShowDevGizmos && parent.Spawned)
        {
            yield return new Command_Action { defaultLabel = "DEV: Arrive by shuttle (" + Props.channel + ")", action = () => Arrive(true) };
            yield return new Command_Action { defaultLabel = "DEV: Arrive by pod (" + Props.channel + ")", action = () => Arrive(false) };
        }
    }

    public override void OpenDialogue(Pawn pawn)
    {
        if (!contact || Busy)
        {
            return;
        }
        string p = Props.dialoguePrefix;
        NamedArgument pawnArg = pawn.Named("PAWN");
        DiaNode? next = null;
        for (int stage = Props.dialogueStages; stage >= 1; stage--)
        {
            DiaNode node = new DiaNode((p + "_Stage" + stage).Translate(pawnArg));
            if (next == null)
            {
                node.options.Add(new DiaOption((p + "_Final").Translate()) { action = Arrive, resolveTree = true });
            }
            else
            {
                node.options.Add(new DiaOption("BOR_Signals_EndConversation".Translate()) { resolveTree = true });
                node.options.Add(new DiaOption((p + "_Ask").Translate()) { link = next });
            }
            next = node;
        }
        Find.WindowStack.Add(new Dialog_NodeTree(next!, true, false, Props.tooltipKey.Translate()));
    }

    protected override void Arrive() => Arrive(!BadzaariPresent);

    private void Arrive(bool byShuttle)
    {
        if (!parent.Spawned || Props.reward == null || Props.visitorKind == null || Props.faction == null)
        {
            return;
        }
        Map map = parent.Map;
        IntVec3 cell;
        if (byShuttle)
        {
            List<IntVec3> spots = SignalUtility.RaidLandingSpots(map, BOR_SignalTenebralDefOf.BOR_TenebralShuttle.size, 1, 0f);
            if (spots.Count == 0)
            {
                Messages.Message("BOR_Raid_NoLandingSpot".Translate(), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }
            cell = spots[0];
        }
        else
        {
            IntVec3 home = map.areaManager.Home.ActiveCells.Where(c => !c.Fogged(map)).RandomElementWithFallback(parent.Position);
            cell = DropCellFinder.TryFindDropSpotNear(home, map, out IntVec3 spot, false, true) ? spot : home;
        }

        Faction faction = Find.FactionManager.FirstFactionOfDef(Props.faction) ?? CreateFaction(Props.faction);

        if (byShuttle)
        {
            Building_RaidShuttle shuttle = (Building_RaidShuttle)ThingMaker.MakeThing(BOR_SignalTenebralDefOf.BOR_TenebralShuttle);
            shuttle.Init(faction, new List<PawnKindDef> { Props.visitorKind }, Props.shuttleReward, Props.reward);
            SkyfallerMaker.SpawnSkyfaller(BOR_SignalTenebralDefOf.BOR_TenebralShuttleIncoming, shuttle, cell, map);
            if (Props.shuttleReward != null)
            {
                Raids.Register(Props.shuttleReward, new List<Thing> { shuttle });
            }
        }
        else
        {
            Pawn pawn = PawnGenerator.GeneratePawn(Props.visitorKind, faction);
            pawn.inventory.innerContainer.TryAdd(ThingMaker.MakeThing(Props.reward));
            ActiveTransporter pod = (ActiveTransporter)ThingMaker.MakeThing(ThingDefOf.ActiveDropPod);
            pod.Contents = new ActiveTransporterInfo { leaveSlag = true };
            pod.Contents.innerContainer.TryAdd(pawn);
            SkyfallerMaker.SpawnSkyfaller(BOR_SignalTenebralDefOf.BOR_TenebralPodIncoming, pod, cell, map);
            LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction), map, new List<Pawn> { pawn });
            visitor = pawn;
        }

        contact = false;
        Find.LetterStack.ReceiveLetter((Props.dialoguePrefix + "_Ambush").Translate(), "BOR_Tenebral_ArrivalText".Translate(faction.Named("FACTION")),
            LetterDefOf.ThreatBig, new TargetInfo(cell, map));
    }

    // The faction is missing from this world: make it, and give it a home by taking over a settlement.
    private static Faction CreateFaction(FactionDef def)
    {
        Faction faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(def));
        Find.FactionManager.Add(faction);

        List<Settlement> others = Find.WorldObjects.Settlements
            .Where(s => s.Faction != null && !s.Faction.IsPlayer && s.Faction != faction && !s.HasMap).ToList();
        // Prefer factions that would keep at least one other settlement.
        List<Settlement> candidates = others.Where(s => others.Count(o => o.Faction == s.Faction) >= 2).ToList();
        Settlement? victim = (candidates.Count > 0 ? candidates : others).RandomElementWithFallback();
        if (victim != null)
        {
            string oldName = victim.LabelCap;
            victim.SetFaction(faction);
            victim.Name = SettlementNameGenerator.GenerateSettlementName(victim, faction.def.settlementNameMaker);
            Find.LetterStack.ReceiveLetter("BOR_Tenebral_SlaughterLabel".Translate(), "BOR_Tenebral_SlaughterText".Translate(oldName),
                LetterDefOf.NegativeEvent, victim);
        }
        else
        {
            Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
            settlement.SetFaction(faction);
            settlement.Tile = TileFinder.RandomSettlementTileFor(faction);
            settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, faction.def.settlementNameMaker);
            Find.WorldObjects.Add(settlement);
        }
        return faction;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref visitor, "signalTenebralVisitor_" + Props.channel);
    }
}
