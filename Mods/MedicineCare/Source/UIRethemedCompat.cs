using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach.MedicineCare;

// [LFS] UI Rethemed - Inspector Tabs ships its own modded-medicine levels, also numbered from 5, but built from a different
// list (it includes ultratech and drops the strongest medicine). Both mods answer GetLabel/AllowsMedicine for the same
// enum values, so labels came from our list while icons and order came from theirs. Point its lookups at CareTiers so
// there is a single source of truth.
public static class UIRethemedCompat
{
    const string TypeName = "UIRethemed.InspectorTabs.ModdedMedicine";

    public static void Apply(Harmony harmony)
    {
        Type? type = AccessTools.TypeByName(TypeName);
        if (type == null)
        {
            return;
        }
        Patch(harmony, type, "MedicineFor", nameof(MedicineFor));
        Patch(harmony, type, "LevelFor", nameof(LevelFor));
        Patch(harmony, type, "AllowsMedicine", nameof(AllowsMedicine));
        Patch(harmony, type, "EnsureVanillaIcons", nameof(SkipOriginal));
        MethodInfo? choices = AccessTools.Method(type, "Choices");
        if (choices != null)
        {
            harmony.Patch(choices, postfix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(Choices)));
        }
    }

    static void Patch(Harmony harmony, Type type, string method, string prefix)
    {
        MethodInfo? target = AccessTools.Method(type, method);
        if (target == null)
        {
            Log.Warning($"[BOR] UI Rethemed compat: {TypeName}.{method} not found");
            return;
        }
        harmony.Patch(target, prefix: new HarmonyMethod(typeof(UIRethemedCompat), prefix));
    }

    static bool MedicineFor(MedicalCareCategory care, ref ThingDef? __result)
    {
        __result = CareTiers.IsExtended(care) && care != MedicalCareCategory.Best ? CareTiers.Get(care).Medicine : null;
        return false;
    }

    static bool LevelFor(string defName, MedicalCareCategory fallback, ref MedicalCareCategory __result)
    {
        foreach (CareTier tier in CareTiers.ByValue)
        {
            if (tier.Care > MedicalCareCategory.Best && tier.Medicine?.defName == defName)
            {
                __result = tier.Care;
                return false;
            }
        }
        __result = fallback;
        return false;
    }

    static bool AllowsMedicine(MedicalCareCategory care, ThingDef medicine, ref bool __result)
    {
        __result = MedicalCareUtility.AllowsMedicine(care, medicine);
        return false;
    }

    // Our icon array is already installed, theirs is sized for their own list.
    static bool SkipOriginal() => false;

    // Only widen the list when its modded-medicine setting is on (it returned more than the five vanilla levels).
    static void Choices(ref List<MedicalCareCategory> __result)
    {
        if (__result.Count > 5)
        {
            __result = CareTiers.ByDisplay.Select(t => t.Care).ToList();
        }
    }
}
