using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// The trader's shuttle: sells one data drive for sunsteel, and leaves angry if refused or ignored.
public class Building_SignalShuttle : Building
{
    private Thing? analyzer;
    private int ticksLeft;
    private int price;
    private bool departed;
    private bool farewellOpen;

    public void Init(CompStrangeSignals comp, int waitTicks, int sunsteelPrice)
    {
        analyzer = comp.parent;
        ticksLeft = waitTicks;
        price = sunsteelPrice;
    }

    public override AcceptanceReport DeconstructibleBy(Faction faction) => false;

    public override AcceptanceReport ClaimableBy(Faction by) => false;

    protected override void Tick()
    {
        base.Tick();
        if (departed || farewellOpen)
        {
            return;
        }
        ticksLeft--;
        if (ticksLeft <= 0)
        {
            OpenFarewell();
        }
    }

    private static int SunsteelOnMap(Map map) => map.listerThings.ThingsOfDef(BOR_SignalsDefOf.BOR_Sunsteel).Sum(t => t.stackCount);

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo g in base.GetGizmos())
        {
            yield return g;
        }
        if (departed)
        {
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "BOR_Signals_TalkToPilot".Translate(),
            defaultDesc = "BOR_Signals_TalkToPilotDesc".Translate(price, ticksLeft.ToStringTicksToPeriod()),
            icon = BOR_SignalsDefOf.BOR_DataDrive.uiIcon,
            action = OpenPawnMenu
        };
        if (DebugSettings.ShowDevGizmos && !farewellOpen)
        {
            yield return new Command_Action { defaultLabel = "DEV: Time out now", action = () => ticksLeft = 1 };
        }
    }

    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (FloatMenuOption o in base.GetFloatMenuOptions(selPawn))
        {
            yield return o;
        }
        if (departed || !selPawn.IsColonistPlayerControlled)
        {
            yield break;
        }
        if (!selPawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
        {
            yield return new FloatMenuOption("BOR_Signals_TalkToPilot".Translate() + " (" + "NoPath".Translate() + ")", null);
            yield break;
        }
        yield return new FloatMenuOption("BOR_Signals_TalkToPilot".Translate(), () => OrderTalk(selPawn));
    }

    private void OpenPawnMenu()
    {
        List<FloatMenuOption> options = new();
        foreach (Pawn p in Map.mapPawns.FreeColonistsSpawned)
        {
            Pawn pawn = p;
            if (pawn.Downed || !pawn.CanReach(this, PathEndMode.Touch, Danger.Deadly))
            {
                options.Add(new FloatMenuOption(pawn.LabelShortCap + " (" + "BOR_Signals_Incapable".Translate() + ")", null));
                continue;
            }
            options.Add(new FloatMenuOption(pawn.LabelShortCap, () => OrderTalk(pawn)));
        }
        if (options.Count == 0)
        {
            Messages.Message("BOR_Signals_NoPawns".Translate(), this, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OrderTalk(Pawn pawn)
    {
        if (!departed && Spawned && pawn.Spawned && !pawn.Downed)
        {
            pawn.jobs?.TryTakeOrderedJob(JobMaker.MakeJob(BOR_SignalsDefOf.BOR_TalkToShuttlePilot, this), JobTag.Misc);
        }
    }

    public void OpenPilotDialogue(Pawn pawn)
    {
        if (departed || farewellOpen || !Spawned)
        {
            return;
        }
        DiaNode offer = new DiaNode("BOR_Signals_PilotOffer".Translate(price, pawn.Named("PAWN")));
        DiaNode farewell = FarewellNode();
        DiaNode thanks = new DiaNode("BOR_Signals_Thanks".Translate());
        thanks.options.Add(new DiaOption("BOR_Signals_Close".Translate()) { resolveTree = true });
        DiaOption yes = new DiaOption("BOR_Signals_Yes".Translate()) { action = () => Buy(pawn), link = thanks };
        if (SunsteelOnMap(Map) < price)
        {
            yes.Disable("BOR_Signals_NotEnoughSunsteel".Translate(price));
        }
        offer.options.Add(yes);
        offer.options.Add(new DiaOption("BOR_Signals_No".Translate()) { action = RefuseOffer, link = farewell });
        Find.WindowStack.Add(new Dialog_NodeTree(offer, true, false, "BOR_Signals_PilotTitle".Translate()));
    }

    private void RefuseOffer()
    {
        farewellOpen = true;
        ticksLeft = 0;
    }

    private void OpenFarewell()
    {
        farewellOpen = true;
        Find.WindowStack.Add(new Dialog_NodeTree(FarewellNode(), true, false, "BOR_Signals_PilotTitle".Translate()));
    }

    private DiaNode FarewellNode()
    {
        DiaNode farewell = new DiaNode("BOR_Signals_Farewell".Translate());
        farewell.options.Add(new DiaOption("BOR_Signals_Close".Translate()) { action = () => Depart(true), resolveTree = true });
        return farewell;
    }

    private void Buy(Pawn pawn)
    {
        if (departed || !Spawned || SunsteelOnMap(Map) < price)
        {
            return;
        }
        int remaining = price;
        foreach (Thing t in Map.listerThings.ThingsOfDef(BOR_SignalsDefOf.BOR_Sunsteel).ToList())
        {
            int take = Mathf.Min(remaining, t.stackCount);
            t.SplitOff(take).Destroy();
            remaining -= take;
            if (remaining <= 0)
            {
                break;
            }
        }
        IntVec3 dropAt = pawn.Spawned && pawn.Map == Map ? pawn.Position : InteractionCell;
        GenPlace.TryPlaceThing(ThingMaker.MakeThing(BOR_SignalsDefOf.BOR_DataDrive), dropAt, Map, ThingPlaceMode.Near);
        Depart(false);
    }

    private void Depart(bool refused)
    {
        if (departed || !Spawned)
        {
            return;
        }
        departed = true;
        Map map = Map;
        IntVec3 pos = Position;
        (analyzer?.TryGetComp<CompStrangeSignals>())?.Notify_ShuttleLeft();
        if (refused)
        {
            map.GetComponent<MapComponent_SignalConsequences>().Schedule();
        }
        Destroy(DestroyMode.Vanish);
        SkyfallerMaker.SpawnSkyfaller(BOR_SignalsDefOf.BOR_SignalShuttleLeaving, pos, map);
    }

    public override string GetInspectString()
    {
        string s = base.GetInspectString();
        if (!departed)
        {
            s += (s.NullOrEmpty() ? "" : "\n") + "BOR_Signals_ShuttleLeavesIn".Translate(ticksLeft.ToStringTicksToPeriod());
        }
        return s;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref analyzer, "analyzer");
        Scribe_Values.Look(ref ticksLeft, "ticksLeft");
        Scribe_Values.Look(ref price, "price");
        Scribe_Values.Look(ref departed, "departed");
        // Open windows are not saved: farewellOpen stays false after loading, and ticksLeft <= 0 reopens the farewell.
    }
}
