using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach.ResearchCommission;

/// <summary>
/// Research Commission (mrk.researchcommission) lets any friendly faction buy or sell research. BOR factions never trade
/// research with the player and BOR research can only be earned in the colony, so both are removed from the mod's
/// eligibility checks, unless the "Research Commission leaves BOR out" mod setting is off. This assembly only loads when
/// that mod is active (see LoadFolders.xml).
/// </summary>
[StaticConstructorOnStartup]
public static class ResearchCommissionCompat
{
    private const string BorPackageId = "bambaryla.bor.redux";

    static ResearchCommissionCompat()
    {
        try
        {
            System.Type generator = AccessTools.TypeByName("ResearchCommission.CommissionGenerator");
            Harmony harmony = new("bambaryla.bor.redux.researchcommission");
            harmony.Patch(AccessTools.Method(generator, "IsEligibleClient"),
                postfix: new HarmonyMethod(typeof(ResearchCommissionCompat), nameof(IsEligibleClientPostfix)));
            harmony.Patch(AccessTools.Method(generator, "IsRegularResearch"),
                postfix: new HarmonyMethod(typeof(ResearchCommissionCompat), nameof(IsRegularResearchPostfix)));
            // The blacklist window lists every project; BOR ones are never offered, so hide them to avoid confusion.
            harmony.Patch(AccessTools.Method("ResearchCommission.Dialog_ResearchBlacklist:PreOpen"),
                postfix: new HarmonyMethod(typeof(ResearchCommissionCompat), nameof(BlacklistPreOpenPostfix)));
        }
        catch (System.Exception e)
        {
            Log.Error($"[BOR] Could not apply the Research Commission compatibility patch. {e}");
        }
    }

    // The setting is read on every check, so toggling it applies at once.
    private static bool IsBor(Def? def) =>
        BorSettings.ResearchCommissionExcludesBor
        && def?.modContentPack != null
        && string.Equals(def.modContentPack.PackageId, BorPackageId, System.StringComparison.OrdinalIgnoreCase);

    public static void IsEligibleClientPostfix(Faction? faction, ref bool __result)
    {
        if (__result && IsBor(faction?.def))
        {
            __result = false;
        }
    }

    public static void IsRegularResearchPostfix(ResearchProjectDef? project, ref bool __result)
    {
        if (__result && IsBor(project))
        {
            __result = false;
        }
    }

    public static void BlacklistPreOpenPostfix(object __instance)
    {
        if (Traverse.Create(__instance).Field("entries").GetValue() is not System.Collections.IList entries)
        {
            return;
        }
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            string? defName = Traverse.Create(entries[i]).Field("defName").GetValue<string>();
            if (defName != null && IsBor(DefDatabase<ResearchProjectDef>.GetNamedSilentFail(defName)))
            {
                entries.RemoveAt(i);
            }
        }
    }
}
