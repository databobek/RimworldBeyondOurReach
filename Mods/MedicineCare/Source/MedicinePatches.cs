using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach.MedicineCare;

[HarmonyPatch(typeof(MedicalCareUtility), nameof(MedicalCareUtility.AllowsMedicine))]
public static class Patch_AllowsMedicine
{
    // Best is no longer "everything": it is the ultratech level, and modded medicine gets its own levels above it.
    public static bool Prefix(MedicalCareCategory cat, ThingDef meds, ref bool __result)
    {
        if (!CareTiers.IsExtended(cat))
        {
            return true;
        }
        __result = meds.GetStatValueAbstract(StatDefOf.MedicalPotency) <= CareTiers.Get(cat).Potency;
        return false;
    }
}

[HarmonyPatch(typeof(MedicalCareUtility), nameof(MedicalCareUtility.GetLabel))]
public static class Patch_GetLabel
{
    public static bool Prefix(MedicalCareCategory cat, ref string __result)
    {
        ThingDef? medicine = CareTiers.IsExtended(cat) ? CareTiers.Get(cat).Medicine : null;
        if (medicine == null)
        {
            return true;
        }
        __result = "BOR_MedicineCare_OrWorse".Translate(medicine.LabelCap);
        return false;
    }
}

[HarmonyPatch(typeof(MedicalCareUtility), nameof(MedicalCareUtility.MedicalCareSetter))]
public static class Patch_MedicalCareSetter
{
    // Tooltip keys only need to be unique per hover area.
    const int TipKeyBase = 0x42_4D_43_00;

    static bool painting;

    public static bool Prefix(Rect rect, ref MedicalCareCategory medCare)
    {
        CareTier[] tiers = CareTiers.ByDisplay;
        Widgets.DrawHighlightIfMouseover(rect);

        // Square icons; wrap onto extra rows once a single row would make them too small.
        int rows = 1;
        while (rows < 4 && rect.height / rows * tiers.Length > rect.width * rows)
        {
            rows++;
        }
        int perRow = Mathf.CeilToInt((float)tiers.Length / rows);
        float size = Mathf.Min(rect.height / rows, rect.width / perRow);
        float left = rect.x + (rect.width - size * perRow) / 2f;
        float top = rect.y + (rect.height - size * rows) / 2f;

        for (int i = 0; i < tiers.Length; i++)
        {
            CareTier tier = tiers[i];
            var cell = new Rect(left + size * (i % perRow), top + size * (i / perRow), size, size);

            bool isSelected = tier.Care == medCare;
            if (isSelected)
            {
                Widgets.DrawBox(cell);
            }
            Widgets.DrawHighlightIfMouseover(cell);
            GUI.DrawTexture(cell, tier.Icon);

            Widgets.DraggableResult result = Widgets.ButtonInvisibleDraggable(cell);
            if (result == Widgets.DraggableResult.Dragged)
            {
                painting = true;
            }
            bool paintedOver = painting && Mouse.IsOver(cell) && !isSelected;
            if (paintedOver || result is Widgets.DraggableResult.Pressed or Widgets.DraggableResult.DraggedThenPressed)
            {
                medCare = tier.Care;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            TooltipHandler.TipRegion(cell, () => tier.Care.GetLabel(), TipKeyBase + (int)tier.Care);
        }

        if (!Input.GetMouseButton(0))
        {
            painting = false;
        }
        return false;
    }
}

[HarmonyPatch(typeof(MedicalCareUtility), nameof(MedicalCareUtility.MedicalCareSelectButton))]
public static class Patch_MedicalCareSelectButton
{
    public static bool Prefix(Rect rect, Pawn pawn)
    {
        Widgets.Dropdown(rect, pawn, p => p.playerSettings.medCare, Menu, buttonIcon: CareTiers.Get(pawn.playerSettings.medCare).Icon, paintable: true);
        return false;
    }

    static IEnumerable<Widgets.DropdownMenuElement<MedicalCareCategory>> Menu(Pawn pawn)
    {
        foreach (CareTier tier in CareTiers.ByDisplay)
        {
            MedicalCareCategory care = tier.Care;
            yield return new Widgets.DropdownMenuElement<MedicalCareCategory>
            {
                option = new FloatMenuOption(care.GetLabel(), () => pawn.playerSettings.medCare = care, tier.Icon, Color.white),
                payload = care,
            };
        }
        yield return new Widgets.DropdownMenuElement<MedicalCareCategory>
        {
            option = new FloatMenuOption("ChangeDefaults".Translate(), () => Find.WindowStack.Add(new Dialog_MedicalDefaults())),
        };
    }
}
