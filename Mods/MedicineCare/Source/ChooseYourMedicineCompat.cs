using System.Diagnostics;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach.MedicineCare
{
    // Choose Your Medicine (Kopp.ChooseYourMedicine) decides whether to use Mod Medicine Patch by looking for an active
    // mod with that name, then reads the patch's data by reflection on "ModMedicinePatch.ModMedicinePatch". There's no
    // such mod here, so this answers that lookup for it and exposes the same reflection surface (see the facade below).
    public class MedicineCareMod : Mod
    {
        const string ForkName = "Mod Medicine Patch 1.6";
        const string ChooseYourMedicineAssembly = "ChooseYourMedicine";

        public MedicineCareMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("bambaryla.bor.redux.medicinecare");
            harmony.PatchAll(typeof(MedicineCareMod).Assembly);
            harmony.Patch(
                AccessTools.Method(typeof(ModLister), nameof(ModLister.HasActiveModWithName)),
                postfix: new HarmonyMethod(typeof(MedicineCareMod), nameof(NamePostfix)));
        }

        // Only lies to Choose Your Medicine itself, and only while it is installed; other callers see the truth.
        static void NamePostfix(string name, ref bool __result)
        {
            if (__result || name != ForkName)
            {
                return;
            }
            var trace = new StackTrace(2, false);
            for (int i = 0; i < trace.FrameCount; i++)
            {
                if (trace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly.GetName().Name == ChooseYourMedicineAssembly)
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}

// Reflection surface expected by Choose Your Medicine; names and shapes must stay as they are.
namespace ModMedicinePatch
{
    public struct ModMedicine
    {
        public MedicalCareCategory care;
        public float potency;
        public ThingDef? thingDef;
        public Texture2D tex;
    }

    public static class ModMedicinePatch
    {
        public static List<ModMedicine> medList => BeyondOurReach.MedicineCare.CareTiers.ByDisplay
            .Select(t => new ModMedicine { care = t.Care, potency = t.Potency, thingDef = t.Medicine, tex = t.Icon })
            .ToList();

        public static List<MedicalCareCategory> GetOrderedCareList() => BeyondOurReach.MedicineCare.CareTiers.ByDisplay.Select(t => t.Care).ToList();

        public static ModMedicine GetMedicineByCare(MedicalCareCategory care)
        {
            var t = BeyondOurReach.MedicineCare.CareTiers.Get(care);
            return new ModMedicine { care = t.Care, potency = t.Potency, thingDef = t.Medicine, tex = t.Icon };
        }

        public static Texture2D GetCareTexture(MedicalCareCategory care) => BeyondOurReach.MedicineCare.CareTiers.Get(care).Icon;
    }
}
