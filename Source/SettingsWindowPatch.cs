using HarmonyLib;
using ModSettingsFramework;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

/// <summary>
/// The framework's restart notice wraps over the first settings section. Pushes BOR's settings down to clear it,
/// without touching the framework itself.
/// </summary>
[StaticConstructorOnStartup]
public static class SettingsWindowPatch
{
    private const string PackageId = "bambaryla.bor.redux";
    private const float TopOffset = 30f;

    static SettingsWindowPatch()
    {
        new Harmony("bambaryla.bor.redux.settingswindow").PatchAll(typeof(SettingsWindowPatch).Assembly);
    }

    [HarmonyPatch(typeof(ModSettingsContainer), nameof(ModSettingsContainer.DoSettingsWindowContents))]
    public static class ModSettingsContainer_DoSettingsWindowContents_Patch
    {
        public static void Prefix(ModSettingsContainer __instance, ref Rect inRect)
        {
            if (string.Equals(__instance.packageID, PackageId, System.StringComparison.OrdinalIgnoreCase))
            {
                inRect.y += TopOffset;
                inRect.height -= TopOffset;
            }
        }
    }
}
