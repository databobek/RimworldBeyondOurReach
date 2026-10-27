using HarmonyLib;
using Verse;

namespace BeyondOurReach.Android;

[StaticConstructorOnStartup]
public static class AndroidMedicineMod
{
    static AndroidMedicineMod()
    {
        new Harmony("bambaryla.bor.redux.android").PatchAll(typeof(AndroidMedicineMod).Assembly);
    }
}
