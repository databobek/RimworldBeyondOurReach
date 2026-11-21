using ModSettingsFramework;

namespace BeyondOurReach;

// BOR's mod settings. Vanilla Expanded Framework's mod settings framework draws them from the
// ModSettingsFramework.PatchOperationModOption patches in the ModSettings_*.xml files. Those options patch nothing; the
// code reads them here, so a change applies at once, without a restart. Each default must match its patch's defaultValue.
public static class BorSettings
{
    private const string PackageId = "bambaryla.bor.redux";

    private static ModSettingsContainer? container;

    // Off: the neutronium forges refuse to analyze a techprof subpersona core, so it cannot be replicated.
    public static bool ReplicateTechprof => Enabled("BOR_ReplicateTechprof", false);

    // On: anti-air only downs drop pods of factions up to one tier above its own. Off: it downs every pod, as in vanilla.
    public static bool AntiAirTiers => Enabled("BOR_AntiAirTiers", true);

    // On: BOR factions and BOR research are left out of Research Commission's trades.
    public static bool ResearchCommissionExcludesBor => Enabled("BOR_ResearchCommissionExcludesBor", true);

    private static bool Enabled(string id, bool defaultValue)
    {
        container ??= ModSettingsFrameworkSettings.GetModSettingsContainer(PackageId);
        return container?.PatchOperationEnabled(id, defaultValue) ?? defaultValue;
    }
}
