using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace BeyondOurReach;

[DefOf]
public static class BOR_SignalRaidDefOf
{
    public static ThingDef BOR_RaidShuttle = null!;
    public static ThingDef BOR_RaidShuttleIncoming = null!;

    static BOR_SignalRaidDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_SignalRaidDefOf));
    }
}

public class CompProperties_SignalRaid : CompProperties
{
    // Unique per analyzer; keys the saved state, since one analyzer can carry several of these comps.
    public string channel = "";
    public string tooltipKey = "";
    // Icon under BOR/UI/Icons for the investigate button.
    public string? investigateIcon;
    public string reportKey = "";
    // Dialogue keys are dialoguePrefix + _Greeting, _HungUp, _Ask, _AskReply, _GiveInfo, _GiveInfoReply, _GiveCoords,
    // _DontGiveCoords; _Ambush is the raid letter's title. Greeting text gets {PAWN_*} and {IMPERSONATED} (the
    // impersonated faction's name).
    public string dialoguePrefix = "";
    public FactionDef? impersonatedFaction;
    public ThingDef? reward;
    public float investigateHours = 4f;
    public FactionDef? faction;
    public List<PawnKindDef> raiderKinds = new();
    public PawnKindDef? leaderKind;
    public int raiderCount = 7;
    public int shuttleCount = 3;

    public CompProperties_SignalRaid()
    {
        compClass = typeof(CompSignalRaid);
    }
}

// Signal channel that ends in a raid: investigate -> dialogue -> shuttles land with raiders -> beat them,
// deconstruct the shuttles, one of them holds the reward. Repeatable until the reward has been analyzed.
public class CompSignalRaid : ThingComp, ISignalChannel
{
    protected bool contact;

    public CompProperties_SignalRaid Props => (CompProperties_SignalRaid)props;

    public bool AwaitingInvestigation => !contact;

    public float InvestigateHours => Props.investigateHours;

    public string InvestigateReport => Props.reportKey.Translate();

    public void Notify_Investigated()
    {
        contact = true;
    }

    protected MapComponent_SignalRaids Raids => parent.Map.GetComponent<MapComponent_SignalRaids>();

    protected Faction? RaidFaction => Props.faction != null ? Find.FactionManager.FirstFactionOfDef(Props.faction) : null;

    // No raiding faction in this world (e.g. removed at world creation): the channel does not exist.
    protected virtual bool ChannelExists => RaidFaction != null;

    // Everything this channel can hand out; the channel ends once all of it has been analyzed.
    protected virtual IEnumerable<ThingDef> Rewards
    {
        get
        {
            if (Props.reward != null)
            {
                yield return Props.reward;
            }
        }
    }

    private bool Completed => Rewards.All(SignalUtility.Analyzed);

    protected virtual bool Busy => Raids.Active(Props.reward!);

    protected virtual string BusyReason => "BOR_Raid_ShuttlesPresent".Translate();

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (!parent.Spawned || parent.Faction != Faction.OfPlayer || Props.reward == null || !ChannelExists)
        {
            yield break;
        }
        if (Completed)
        {
            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Forget analysis (" + Props.channel + ")",
                    action = () =>
                    {
                        foreach (ThingDef r in Rewards)
                        {
                            Find.AnalysisManager.RemoveAnalysisDetails(SignalUtility.AnalysisID(r));
                        }
                    }
                };
            }
            yield break;
        }

        Command_Action cmd = new Command_Action();
        if (!contact)
        {
            cmd.defaultLabel = "";
            cmd.defaultDesc = Props.tooltipKey.Translate();
            cmd.icon = Props.investigateIcon != null ? BorIcons.Get(Props.investigateIcon) : BaseContent.ClearTex;
            cmd.action = () => SignalUtility.OpenPawnMenu(this, BOR_SignalsDefOf.BOR_InvestigateSignals);
        }
        else
        {
            cmd.defaultLabel = "BOR_Signals_Contact_Label".Translate();
            cmd.defaultDesc = "BOR_Raid_Contact_Desc".Translate(Props.tooltipKey.Translate());
            cmd.icon = Props.reward.uiIcon;
            cmd.action = () => SignalUtility.OpenPawnMenu(this, BOR_SignalsDefOf.BOR_ContactSignals);
        }
        if (!SignalUtility.Powered(parent))
        {
            cmd.Disable("NoPower".Translate());
        }
        else if (Busy)
        {
            cmd.Disable(BusyReason);
        }
        yield return cmd;

        if (DebugSettings.ShowDevGizmos)
        {
            if (!contact)
            {
                yield return new Command_Action { defaultLabel = "DEV: Finish investigation (" + Props.channel + ")", action = Notify_Investigated };
            }
            yield return new Command_Action { defaultLabel = "DEV: Arrive (" + Props.channel + ")", action = Arrive };
        }
    }

    public virtual void OpenDialogue(Pawn pawn)
    {
        if (!contact || Busy)
        {
            return;
        }
        string p = Props.dialoguePrefix;
        NamedArgument pawnArg = pawn.Named("PAWN");
        NamedArgument impersonated = ImpersonatedName().Named("IMPERSONATED");

        DiaNode hungUp = new DiaNode((p + "_HungUp").Translate(pawnArg));
        hungUp.options.Add(new DiaOption("BOR_Signals_Close".Translate()) { resolveTree = true });

        DiaNode coords = new DiaNode((p + "_GiveInfoReply").Translate(pawnArg));
        coords.options.Add(new DiaOption((p + "_GiveCoords").Translate()) { action = Arrive, resolveTree = true });
        coords.options.Add(new DiaOption((p + "_DontGiveCoords").Translate()) { link = hungUp });

        DiaNode ask = new DiaNode((p + "_AskReply").Translate(pawnArg));
        ask.options.Add(new DiaOption((p + "_GiveInfo").Translate()) { link = coords });
        ask.options.Add(new DiaOption("BOR_Signals_EndConversation".Translate()) { link = hungUp });

        DiaNode greeting = new DiaNode((p + "_Greeting").Translate(pawnArg, impersonated));
        greeting.options.Add(new DiaOption("BOR_Signals_EndConversation".Translate()) { link = hungUp });
        greeting.options.Add(new DiaOption((p + "_Ask").Translate()) { link = ask });

        Find.WindowStack.Add(new Dialog_NodeTree(greeting, true, false, Props.tooltipKey.Translate()));
    }

    private string ImpersonatedName()
    {
        if (Props.impersonatedFaction == null)
        {
            return "";
        }
        return Find.FactionManager.FirstFactionOfDef(Props.impersonatedFaction)?.Name ?? Props.impersonatedFaction.LabelCap.ToString();
    }

    // The "come" decision.
    protected virtual void Arrive()
    {
        if (!parent.Spawned || Props.reward == null)
        {
            return;
        }
        Map map = parent.Map;
        Faction? faction = RaidFaction;
        List<IntVec3> spots = SignalUtility.RaidLandingSpots(map, BOR_SignalRaidDefOf.BOR_RaidShuttle.size, Props.shuttleCount, 15f);
        if (faction == null)
        {
            return;
        }
        if (spots.Count == 0)
        {
            Messages.Message("BOR_Raid_NoLandingSpot".Translate(), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }

        List<PawnKindDef> kinds = new();
        if (Props.leaderKind != null)
        {
            kinds.Add(Props.leaderKind);
        }
        for (int i = 0; i < Props.raiderCount && Props.raiderKinds.Count > 0; i++)
        {
            kinds.Add(Props.raiderKinds.RandomElement());
        }

        int carrier = Rand.Range(0, spots.Count);
        List<Thing> shuttles = new();
        for (int s = 0; s < spots.Count; s++)
        {
            List<PawnKindDef> crew = kinds.Where((_, i) => i % spots.Count == s).ToList();
            Building_RaidShuttle shuttle = (Building_RaidShuttle)ThingMaker.MakeThing(BOR_SignalRaidDefOf.BOR_RaidShuttle);
            shuttle.Init(faction, crew, s == carrier ? Props.reward : null);
            SkyfallerMaker.SpawnSkyfaller(BOR_SignalRaidDefOf.BOR_RaidShuttleIncoming, shuttle, spots[s], map);
            shuttles.Add(shuttle);
        }
        Raids.Register(Props.reward, shuttles);
        contact = false;
        Find.LetterStack.ReceiveLetter((Props.dialoguePrefix + "_Ambush").Translate(), "BOR_Raid_ArrivalText".Translate(faction.Named("FACTION")),
            LetterDefOf.ThreatBig, new LookTargets(spots.Select(c => new TargetInfo(c, map))));
    }

    public override string CompInspectStringExtra() =>
        contact && Props.reward != null && ChannelExists && !Completed ? "BOR_Signals_InspectContact".Translate().Resolve() : null!;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref contact, "signalRaidContact_" + Props.channel);
    }
}

// Raider shuttle: its crew files out of the door one by one shortly after landing; it is locked while the raid is on,
// and once the raid is beaten becomes the colony's to deconstruct. The carrier drops the reward when deconstructed.
public class Building_RaidShuttle : Building
{
    private const int ExitDelayTicks = 90;
    private const int ExitIntervalTicks = 35;

    private Faction? raidFaction;
    private List<PawnKindDef> crew = new();
    private ThingDef? reward;
    private ThingDef? crewItem;
    private int exitTick = -1;
    private int exited;
    private bool released;
    private Lord? lord;

    public Faction? RaidFaction => raidFaction;

    public bool Released => released;

    // carriedReward drops when the shuttle is deconstructed; crewItem goes into the first crew member's inventory.
    public void Init(Faction faction, List<PawnKindDef> kinds, ThingDef? carriedReward, ThingDef? crewItem = null)
    {
        raidFaction = faction;
        crew = kinds;
        reward = carriedReward;
        this.crewItem = crewItem;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!respawningAfterLoad && !released)
        {
            exitTick = Find.TickManager.TicksGame + ExitDelayTicks;
        }
    }

    protected override void Tick()
    {
        base.Tick();
        if (!released && exitTick >= 0 && Find.TickManager.TicksGame >= exitTick)
        {
            ExitNext();
            exitTick = Find.TickManager.TicksGame + ExitIntervalTicks;
        }
    }

    // Door: the cell in front of the middle of the shuttle's south side, or the nearest free cell around the hull.
    private IntVec3 DoorCell()
    {
        CellRect rect = this.OccupiedRect();
        IntVec3 front = new IntVec3(rect.CenterCell.x, 0, rect.minZ - 1);
        if (front.InBounds(Map) && front.Standable(Map))
        {
            return front;
        }
        foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(this).OrderBy(c => c.DistanceToSquared(front)))
        {
            if (c.InBounds(Map) && c.Standable(Map))
            {
                return c;
            }
        }
        return front;
    }

    private void ExitNext()
    {
        if (raidFaction == null || exited >= crew.Count)
        {
            released = true;
            return;
        }
        IntVec3 door = DoorCell();
        Pawn pawn = PawnGenerator.GeneratePawn(crew[exited], raidFaction);
        if (crewItem != null)
        {
            pawn.inventory.innerContainer.TryAdd(ThingMaker.MakeThing(crewItem));
            crewItem = null;
        }
        GenSpawn.Spawn(pawn, door, Map, Rot4.South);
        FleckMaker.ThrowDustPuff(door, Map, 1.2f);
        if (lord == null || !Map.lordManager.lords.Contains(lord))
        {
            lord = LordMaker.MakeNewLord(raidFaction, new LordJob_AssaultColony(raidFaction), Map, new List<Pawn> { pawn });
        }
        else
        {
            lord.AddPawn(pawn);
        }
        exited++;
        released = exited >= crew.Count;
    }

    public override AcceptanceReport DeconstructibleBy(Faction faction) =>
        Faction == Faction.OfPlayer && faction == Faction.OfPlayer ? true : "BOR_Raid_ShuttleLocked".Translate();

    public override AcceptanceReport ClaimableBy(Faction by) => false;

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (mode == DestroyMode.Deconstruct && reward != null && Spawned)
        {
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(reward), Position, Map, ThingPlaceMode.Near);
            Messages.Message("BOR_Raid_RewardFound".Translate(reward.label), new TargetInfo(Position, Map), MessageTypeDefOf.PositiveEvent);
            reward = null;
        }
        base.Destroy(mode);
    }

    public override string GetInspectString()
    {
        string s = base.GetInspectString();
        if (Faction != Faction.OfPlayer)
        {
            s += (s.NullOrEmpty() ? "" : "\n") + "BOR_Raid_ShuttleLocked".Translate();
        }
        return s;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref raidFaction, "raidFaction");
        Scribe_Collections.Look(ref crew, "crew", LookMode.Def);
        Scribe_Defs.Look(ref reward, "reward");
        Scribe_Defs.Look(ref crewItem, "crewItem");
        Scribe_Values.Look(ref exitTick, "exitTick", -1);
        Scribe_Values.Look(ref exited, "exited");
        Scribe_Values.Look(ref released, "released");
        Scribe_References.Look(ref lord, "lord");
        crew ??= new List<PawnKindDef>();
    }
}

// Tracks signal raids per map: hands the shuttles to the colony once the raiders are beaten, and frees the channel
// once every shuttle is gone.
public class MapComponent_SignalRaids : MapComponent
{
    private const int CheckInterval = 250;

    private List<SignalRaid> raids = new();

    public MapComponent_SignalRaids(Map map) : base(map)
    {
    }

    public bool Active(ThingDef reward) => raids.Any(r => r.reward == reward);

    public void Register(ThingDef reward, List<Thing> shuttles)
    {
        raids.Add(new SignalRaid { reward = reward, shuttles = shuttles });
    }

    public override void MapComponentTick()
    {
        if (raids.Count == 0 || Find.TickManager.TicksGame % CheckInterval != 0)
        {
            return;
        }
        for (int i = raids.Count - 1; i >= 0; i--)
        {
            SignalRaid raid = raids[i];
            raid.shuttles.RemoveAll(t => t == null || t.Destroyed);
            if (raid.shuttles.Count == 0)
            {
                raids.RemoveAt(i);
                continue;
            }
            if (!raid.defeated && Defeated(raid))
            {
                raid.defeated = true;
                foreach (Thing t in raid.shuttles)
                {
                    t.SetFaction(Faction.OfPlayer);
                }
                Find.LetterStack.ReceiveLetter("BOR_Raid_DefeatedLabel".Translate(), "BOR_Raid_DefeatedText".Translate(),
                    LetterDefOf.PositiveEvent, new LookTargets(raid.shuttles));
            }
        }
    }

    // Beaten once every shuttle has landed and released its crew, and none of that faction is still fighting on the map.
    private bool Defeated(SignalRaid raid)
    {
        List<Building_RaidShuttle> shuttles = raid.shuttles.OfType<Building_RaidShuttle>().ToList();
        if (shuttles.Count != raid.shuttles.Count || shuttles.Any(s => !s.Spawned || !s.Released))
        {
            return false;
        }
        foreach (Faction? f in shuttles.Select(s => s.RaidFaction).Distinct())
        {
            if (f != null && map.mapPawns.SpawnedPawnsInFaction(f).Any(p => !p.Downed && !p.Dead && p.HostileTo(Faction.OfPlayer)))
            {
                return false;
            }
        }
        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref raids, "signalRaids", LookMode.Deep);
        raids ??= new List<SignalRaid>();
    }

    public class SignalRaid : IExposable
    {
        public ThingDef? reward;
        public List<Thing> shuttles = new();
        public bool defeated;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref reward, "reward");
            Scribe_Collections.Look(ref shuttles, "shuttles", LookMode.Reference);
            Scribe_Values.Look(ref defeated, "defeated");
            shuttles ??= new List<Thing>();
        }
    }
}
