using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

[DefOf]
public static class BOR_DarkMedicineDefOf
{
    public static ThingDef BOR_MedicineIV = null!;
    public static HediffDef BOR_DarkMedicineSickness = null!;

    static BOR_DarkMedicineDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_DarkMedicineDefOf));
    }
}

// Dark medicine runs on nanites of the dark archotech. Anyone it does not already answer to gets sick from them.
public static class DarkMedicine
{
    public static bool IsAttuned(Pawn pawn) =>
        Umbral.IsUmbral(pawn) || TenebralMurder.ImplantCount(pawn) > 0 || Orientis.HasTenebralParts(pawn);

    public static bool IsSick(Pawn pawn) =>
        pawn.health?.hediffSet?.HasHediff(BOR_DarkMedicineDefOf.BOR_DarkMedicineSickness) ?? false;

    public static void Infect(Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike || IsAttuned(pawn))
        {
            return;
        }
        HediffDef def = BOR_DarkMedicineDefOf.BOR_DarkMedicineSickness;
        if (IsSick(pawn))
        {
            // Another dose while sick wipes the immunity built so far.
            ImmunityRecord? record = pawn.health.immunity.GetImmunityRecord(def);
            if (record != null)
            {
                record.immunity = 0f;
            }
            return;
        }
        pawn.health.AddHediff(def);
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class DarkMedicinePatches
{
    [HarmonyPatch(typeof(TendUtility), nameof(TendUtility.DoTend))]
    public static class TendUtility_DoTend_Patch
    {
        // The medicine may be used up inside DoTend, so its def is read first.
        public static void Prefix(Medicine medicine, out bool __state) =>
            __state = medicine?.def == BOR_DarkMedicineDefOf.BOR_MedicineIV;

        public static void Postfix(Pawn patient, bool __state)
        {
            if (__state && patient != null)
            {
                DarkMedicine.Infect(patient);
            }
        }
    }

    // No anaesthesia: the nanites burn it out, so the pawn cannot be put under.
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult))]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        public static bool Prefix(Pawn ___pawn, Hediff hediff) =>
            hediff.def != HediffDefOf.Anesthetic || !DarkMedicine.IsSick(___pawn);
    }

    // Every mood break while sick is a berserk rage.
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public static class MentalStateHandler_TryStartMentalState_DarkMedicine_Patch
    {
        public static void Prefix(Pawn ___pawn, ref MentalStateDef stateDef, bool causedByMood)
        {
            if (causedByMood && stateDef != MentalStateDefOf.Berserk && DarkMedicine.IsSick(___pawn))
            {
                stateDef = MentalStateDefOf.Berserk;
            }
        }
    }
}
