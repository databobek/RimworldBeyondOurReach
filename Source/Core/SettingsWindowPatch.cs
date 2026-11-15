using HarmonyLib;
using Verse;

namespace BeyondOurReach;

/// <summary>
/// Applies every Harmony patch in this assembly.
/// </summary>
[StaticConstructorOnStartup]
public static class SettingsWindowPatch
{
    static SettingsWindowPatch()
    {
        // One class at a time: PatchAll stops at the first target that is missing, and every patch after it is lost.
        Harmony harmony = new("bambaryla.bor.redux.settingswindow");
        foreach (System.Type type in AccessTools.GetTypesFromAssembly(typeof(SettingsWindowPatch).Assembly))
        {
            // Only patch classes. Harmony calls a method named Prepare, TargetMethod or Cleanup on any class it is given,
            // so every JobDriver (which has an instance Cleanup) would fail.
            if (!type.IsDefined(typeof(HarmonyAttribute), false)
                && !AccessTools.GetDeclaredMethods(type).Any(m => m.IsDefined(typeof(HarmonyAttribute), false)))
            {
                continue;
            }
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (System.Exception e)
            {
                Log.Error($"[BOR] Could not apply the patch {type.FullName}; the other patches still load. {e}");
            }
        }
    }
}
