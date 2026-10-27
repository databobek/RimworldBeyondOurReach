using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach.MedicineCare;

public readonly record struct CareTier(MedicalCareCategory Care, ThingDef? Medicine, float Potency, Texture2D Icon);

// Gives every medicine its own medical care level. The vanilla enum values keep their meaning (0..4 = no care, no meds,
// herbal, industrial, ultratech) and modded medicines are appended after them as (MedicalCareCategory)5, 6, ...
// Appended levels are ordered by potency then defName so a saved value keeps pointing at the same medicine.
public static class CareTiers
{
    static CareTier[]? byValue;
    static CareTier[]? byDisplay;

    // Indexed by (int)MedicalCareCategory.
    public static CareTier[] ByValue => byValue ??= Build();

    // Same tiers as ByValue, but the medicine ones are sorted by potency for drawing.
    public static CareTier[] ByDisplay
    {
        get
        {
            if (byDisplay == null)
            {
                CareTier[] all = ByValue;
                byDisplay = all.Take(2).Concat(all.Skip(2).OrderBy(t => t.Potency)).ToArray();
            }
            return byDisplay;
        }
    }

    public static bool IsExtended(MedicalCareCategory cat) => (int)cat >= (int)MedicalCareCategory.Best;

    public static CareTier Get(MedicalCareCategory cat)
    {
        CareTier[] tiers = ByValue;
        int index = (int)cat;
        return tiers[index >= 0 && index < tiers.Length ? index : (int)MedicalCareCategory.NoMeds];
    }

    static CareTier[] Build()
    {
        ThingDef[] vanilla = [ThingDefOf.MedicineHerbal, ThingDefOf.MedicineIndustrial, ThingDefOf.MedicineUltratech];
        List<ThingDef> modded = DefDatabase<ThingDef>.AllDefs
            .Where(d => d.IsMedicine && !vanilla.Contains(d))
            .OrderBy(d => d.GetStatValueAbstract(StatDefOf.MedicalPotency))
            .ThenBy(d => d.defName, StringComparer.Ordinal)
            .ToList();

        List<CareTier> tiers =
        [
            new(MedicalCareCategory.NoCare, null, 0f, ContentFinder<Texture2D>.Get("UI/Icons/Medical/NoCare")),
            new(MedicalCareCategory.NoMeds, null, 0f, ContentFinder<Texture2D>.Get("UI/Icons/Medical/Nomeds")),
        ];
        foreach (ThingDef def in vanilla.Concat(modded))
        {
            tiers.Add(new((MedicalCareCategory)tiers.Count, def, def.GetStatValueAbstract(StatDefOf.MedicalPotency), def.uiIcon));
        }

        AllowPawnsToCarry(modded);
        ReplaceVanillaIcons(tiers);
        Log.Message("[BOR] Medicine care levels: " + string.Join(", ", tiers.Select(t => $"{(int)t.Care}={t.Medicine?.defName ?? t.Care.ToString()} ({t.Potency:0.##})")));
        return tiers.ToArray();
    }

    // Pawns only stock medicine that is in the inventory stock group.
    static void AllowPawnsToCarry(List<ThingDef> modded)
    {
        List<ThingDef>? stocked = InventoryStockGroupDefOf.Medicine.thingDefs;
        if (stocked == null)
        {
            return;
        }
        foreach (ThingDef def in modded.Where(d => !stocked.Contains(d)))
        {
            stocked.Add(def);
        }
    }

    // Vanilla indexes its own icon array with the enum value, so it has to be long enough for the extra levels.
    static void ReplaceVanillaIcons(List<CareTier> tiers)
    {
        var field = HarmonyLib.AccessTools.Field(typeof(MedicalCareUtility), "careTextures");
        if (field?.FieldType == typeof(Texture2D[]))
        {
            field.SetValue(null, tiers.Select(t => t.Icon).ToArray());
        }
    }
}
