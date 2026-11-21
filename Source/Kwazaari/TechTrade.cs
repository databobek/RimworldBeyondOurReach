using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

[DefOf]
public static class BOR_TechTradeDefOf
{
    [MayRequireIdeology]
    public static HistoryEventDef BOR_SoldTechToLesser = null!;

    static BOR_TechTradeDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_TechTradeDefOf));
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
// Kwazaarism's BOR_TechTrade_Abhorrent: selling an item whose tech level is above the buying faction's is recorded
// once per trade, as vanilla records a traded organ. Gifts and factionless (orbital) traders don't count.
[HarmonyPatch(typeof(TradeDeal), nameof(TradeDeal.TryExecute))]
public static class TradeDeal_TryExecute_TechTrade_Patch
{
    // The tradeables are checked before the deal runs, since executing it resets them.
    public static void Prefix(List<Tradeable> ___tradeables, out bool __state)
    {
        TechLevel? buyer = TradeSession.trader?.Faction?.def.techLevel;
        __state = ModsConfig.IdeologyActive && BOR_TechTradeDefOf.BOR_SoldTechToLesser != null && !TradeSession.giftMode
            && buyer != null && ___tradeables.Any(t => t.ActionToDo == TradeAction.PlayerSells && t.ThingDef != null
                && t.ThingDef.techLevel > buyer);
    }

    public static void Postfix(bool __result, ref bool actuallyTraded, bool __state)
    {
        if (__state && __result && actuallyTraded && TradeSession.playerNegotiator != null)
        {
            Find.HistoryEventsManager.RecordEvent(new HistoryEvent(BOR_TechTradeDefOf.BOR_SoldTechToLesser,
                TradeSession.playerNegotiator.Named(HistoryEventArgsNames.Doer)));
        }
    }
}
