using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Pawns lent by the Kwazaari reinforcements favor, and the shuttle that comes to take them back.
public class KwazaariLoan : IExposable
{
    // Pawns still on loan; a pawn is removed once it boards the pickup shuttle.
    public List<Pawn> pawns = new();
    public Map? map;
    public int returnTick;
    public float lostPawnCost;
    public bool pickupSent;

    public void ExposeData()
    {
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        Scribe_References.Look(ref map, "map");
        Scribe_Values.Look(ref returnTick, "returnTick");
        Scribe_Values.Look(ref lostPawnCost, "lostPawnCost");
        Scribe_Values.Look(ref pickupSent, "pickupSent");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pawns ??= new List<Pawn>();
            // A pawn whose corpse was destroyed loads as null; keep the slot so it is still charged as lost.
        }
    }
}

// Scrip balance, favor cooldowns and active loans. Also keeps the faction hidden and neutral.
public class GameComponent_Kwazaari : GameComponent
{
    private const int CheckInterval = 250;
    private const int NeutralityInterval = GenDate.TicksPerHour;

    private float scrip;
    private Dictionary<KwazaariFavorDef, int> nextAvailable = new();
    private List<KwazaariLoan> loans = new();

    private List<KwazaariFavorDef>? tmpKeys;
    private List<int>? tmpValues;

    public GameComponent_Kwazaari(Game game)
    {
    }

    public static GameComponent_Kwazaari Instance => Current.Game.GetComponent<GameComponent_Kwazaari>();

    public static Faction? Faction => Find.FactionManager.FirstFactionOfDef(BOR_KwazaariDefOf.BOR_KwazaarBazaar);

    public float Scrip => scrip;

    public IEnumerable<KwazaariLoan> Loans => loans;

    public void AddScrip(float amount) => scrip += amount;

    public int CooldownLeft(KwazaariFavorDef def) =>
        nextAvailable.TryGetValue(def, out int tick) ? Mathf.Max(0, tick - Find.TickManager.TicksGame) : 0;

    public AcceptanceReport CanBuy(KwazaariFavorDef def, Map map)
    {
        if (!def.PackageActive)
        {
            return "BOR_Kwazaari_FavorNeedsMod".Translate();
        }
        if (scrip < 0f)
        {
            return "BOR_Kwazaari_InDebt".Translate();
        }
        if (scrip < def.cost)
        {
            return "BOR_Kwazaari_NotEnoughScrip".Translate(def.cost.ToString("0.#"));
        }
        int left = CooldownLeft(def);
        if (left > 0)
        {
            return "BOR_Kwazaari_OnCooldown".Translate(left.ToStringTicksToPeriod());
        }
        return def.Worker.CanDeliver(map);
    }

    // Called by a favor worker once the player has finished choosing and the delivery is on its way.
    public bool TryPay(KwazaariFavorDef def, Map map)
    {
        if (!CanBuy(def, map).Accepted)
        {
            return false;
        }
        scrip -= def.cost;
        nextAvailable[def] = Find.TickManager.TicksGame + def.CooldownTicks;
        return true;
    }

    public bool IsBorrowed(Pawn pawn)
    {
        for (int i = 0; i < loans.Count; i++)
        {
            if (loans[i].pawns.Contains(pawn))
            {
                return true;
            }
        }
        return false;
    }

    public bool AnyLoanActive => loans.Count > 0;

    public void AddLoan(KwazaariLoan loan) => loans.Add(loan);

    public void Notify_Returned(Pawn pawn)
    {
        foreach (KwazaariLoan loan in loans)
        {
            loan.pawns.Remove(pawn);
        }
    }

    // The pickup shuttle left: charge for every pawn that did not board, and let those stay with the colony.
    public void Notify_PickupDeparted(KwazaariLoan loan)
    {
        List<Pawn> lost = loan.pawns.ToList();
        loans.Remove(loan);
        if (lost.Count == 0)
        {
            Messages.Message("BOR_Kwazaari_LoanReturned".Translate(), MessageTypeDefOf.PositiveEvent, false);
            return;
        }
        float charge = lost.Count * loan.lostPawnCost;
        scrip -= charge;
        foreach (Pawn p in lost)
        {
            ReleaseFromLoan(p);
        }
        Find.LetterStack.ReceiveLetter("BOR_Kwazaari_LoanLostLabel".Translate(),
            "BOR_Kwazaari_LoanLostText".Translate(lost.Count, charge.ToString("0.#"), scrip.ToString("0.#")),
            LetterDefOf.NegativeEvent);
    }

    // A pawn that stays behind is no longer on loan: its gear is unlocked and the loan marker removed.
    private static void ReleaseFromLoan(Pawn? pawn)
    {
        if (pawn == null || pawn.Dead)
        {
            return;
        }
        pawn.apparel?.UnlockAll();
        Hediff? marker = pawn.health?.hediffSet.GetFirstHediffOfDef(BOR_KwazaariDefOf.BOR_KwazaariLoan);
        if (marker != null)
        {
            pawn.health!.RemoveHediff(marker);
        }
    }

    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        if (now % NeutralityInterval == 0)
        {
            EnsureNeutral();
        }
        if (now % CheckInterval != 0)
        {
            return;
        }
        foreach (KwazaariLoan loan in loans.ToList())
        {
            bool mapGone = loan.map == null || !Find.Maps.Contains(loan.map);
            if (loan.pickupSent)
            {
                // The shuttle closes the loan when it leaves. If its map went first, nothing else would.
                if (mapGone)
                {
                    Notify_PickupDeparted(loan);
                }
                continue;
            }
            if (now < loan.returnTick)
            {
                continue;
            }
            if (mapGone)
            {
                loan.pickupSent = true;
                Notify_PickupDeparted(loan);
            }
            else if (Building_KwazaariShuttle.TrySendPickup(loan))
            {
                loan.pickupSent = true;
            }
            // No landing spot yet: try again on the next check.
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        Faction? faction = EnsureFaction();
        if (faction == null)
        {
            return;
        }
        RemoveWorldObjects(faction);
        EnsureNeutral();
    }

    private static Faction? EnsureFaction()
    {
        Faction? faction = Faction;
        if (faction == null && Find.World != null)
        {
            faction = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(BOR_KwazaariDefOf.BOR_KwazaarBazaar));
            Find.FactionManager.Add(faction);
        }
        if (faction != null)
        {
            faction.hidden = true;
        }
        return faction;
    }

    // Saves from before the rework still have Kwazaari settlements; the faction now lives only off-world.
    private static void RemoveWorldObjects(Faction faction)
    {
        foreach (WorldObject wo in Find.WorldObjects.AllWorldObjects.ToList())
        {
            if (wo.Faction != faction || (wo is MapParent mp && mp.HasMap))
            {
                continue;
            }
            wo.Destroy();
        }
    }

    public static void EnsureNeutral()
    {
        Faction? faction = Faction;
        if (faction == null || Faction.OfPlayerSilentFail == null)
        {
            return;
        }
        FactionRelation? rel = faction.RelationWith(Faction.OfPlayer, true);
        FactionRelation? back = Faction.OfPlayer.RelationWith(faction, true);
        if (rel == null || back == null)
        {
            return;
        }
        rel.baseGoodwill = 0;
        back.baseGoodwill = 0;
        if (rel.kind != FactionRelationKind.Neutral)
        {
            faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Neutral, false);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref scrip, "scrip");
        Scribe_Collections.Look(ref nextAvailable, "nextAvailable", LookMode.Def, LookMode.Value, ref tmpKeys, ref tmpValues);
        Scribe_Collections.Look(ref loans, "loans", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            nextAvailable ??= new Dictionary<KwazaariFavorDef, int>();
            nextAvailable.RemoveAll(kv => kv.Key == null);
            loans ??= new List<KwazaariLoan>();
        }
    }
}
