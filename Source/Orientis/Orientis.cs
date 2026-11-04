using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Tunables for the Path of Light; placed on BOR_Orientis.
public class OrientisPathSettingsDef : Def
{
    // Share of a restored part's health the golden scar takes.
    public float scarHealthFraction = 0.25f;

    // How long the stripping stage lasts. The parts come out halfway through.
    public int stripTicks = 120;

    // How long cleansed pawns glow, counted from the choice. It runs on past the rest of the sequence.
    public int glowTicks = 600;

    // How fast the ring from a struck altar crosses the map. The sequence waits for it before finishing.
    public float shockwaveTilesPerTick = 0.75f;

    // How long the sequence waits on the golden beams before converting anyone.
    public int beamTicks = 60;
}

[DefOf]
public static class BOR_OrientisDefOf
{
    public static OrientisPathSettingsDef BOR_OrientisPath = null!;
    public static HediffDef BOR_Orientis = null!;
    public static HediffDef BOR_GoldenScar = null!;
    public static AbilityDef BOR_OrientisManifest = null!;
    public static LetterDef BOR_OrientisChoice = null!;
    public static StatDef BOR_MeditationStrength = null!;
    public static HediffDef BOR_Blessing = null!;
    public static HediffDef BOR_ShieldExposure = null!;
    public static HediffDef BOR_OrientisArmorBreak = null!;
    public static HediffDef BOR_TenebralPsychosis = null!;
    public static JobDef BOR_OrientisMeditate = null!;
    public static JobDef BOR_OrientisBless = null!;
    public static JobDef BOR_OrientisEnableShield = null!;
    public static WorkTypeDef BOR_Meditate = null!;

    [MayRequireBiotech]
    public static XenotypeDef BOR_OrientisXenotype = null!;

    static BOR_OrientisDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_OrientisDefOf));
    }
}

public static class Orientis
{
    public static OrientisPathSettingsDef Settings => BOR_OrientisDefOf.BOR_OrientisPath;

    // The BOR_Orientis hediff is the fallback when Biotech (and so the xenotype) is not active.
    public static bool IsOrientis(Pawn pawn) =>
        (pawn.genes != null && BOR_OrientisDefOf.BOR_OrientisXenotype != null && pawn.genes.Xenotype == BOR_OrientisDefOf.BOR_OrientisXenotype)
        || (pawn.health?.hediffSet?.HasHediff(BOR_OrientisDefOf.BOR_Orientis) ?? false);

    // Every living pawn of the colony, wherever it is. Prisoners and guests keep their own faction, so they are not in here.
    public static List<Pawn> ColonyPawns() =>
        PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(p => p.Faction == Faction.OfPlayer && p.RaceProps.Humanlike).ToList();

    private static bool IsTenebralPart(Hediff hediff) =>
        hediff.def == BOR_TenebralDefOf.BOR_Crystallized || hediff.def.HasModExtension<DefModExtension_TenebralImplant>();

    public static bool HasTenebralParts(Pawn pawn) => pawn.health.hediffSet.hediffs.Any(IsTenebralPart);

    // Takes out every tenebral part. Each part underneath comes back natural, with a golden scar.
    public static void Strip(Pawn pawn)
    {
        List<Hediff> parts = pawn.health.hediffSet.hediffs.Where(IsTenebralPart).ToList();
        HashSet<BodyPartRecord> scarred = new();
        foreach (Hediff hediff in parts)
        {
            if (hediff.Part != null)
            {
                scarred.Add(hediff.Part);
            }
            if (hediff is Hediff_AddedPart && hediff.Part != null)
            {
                pawn.health.RestorePart(hediff.Part);
            }
            else if (pawn.health.hediffSet.hediffs.Contains(hediff))
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
        foreach (BodyPartRecord part in scarred)
        {
            AddScar(pawn, part);
        }
    }

    private static void AddScar(Pawn pawn, BodyPartRecord part)
    {
        Hediff_Injury scar = (Hediff_Injury)HediffMaker.MakeHediff(BOR_OrientisDefOf.BOR_GoldenScar, pawn, part);
        scar.Severity = System.Math.Min(part.def.GetMaxHealth(pawn) * Settings.scarHealthFraction, pawn.health.hediffSet.GetPartHealth(part) - 1f);
        if (scar.Severity <= 0f)
        {
            return;
        }
        HediffComp_GetsPermanent? permanent = scar.TryGetComp<HediffComp_GetsPermanent>();
        if (permanent != null)
        {
            permanent.IsPermanent = true;
        }
        pawn.health.AddHediff(scar, part);
    }

    // Turns the pawn from the dark: no psychosis, no break counter, no dark abilities. Then makes it Orientis.
    public static void Convert(Pawn pawn)
    {
        Umbral.RemoveHediffs(pawn, BOR_TenebralDefOf.BOR_TenebralPsychosis);
        Umbral.RemoveHediffs(pawn, BOR_TenebralDefOf.BOR_TenebralBreakCounter);
        Umbral.RemoveHediffs(pawn, BOR_DarkAbilityDefOf.BOR_DarkAbilityHits);
        OrientisRestrictions.DropDarkGear(pawn);
        if (pawn.abilities != null)
        {
            foreach (AbilityDef ability in DarkAbilities.Settings.abilities)
            {
                pawn.abilities.RemoveAbility(ability);
            }
        }
        if (ModsConfig.BiotechActive && pawn.genes != null && BOR_OrientisDefOf.BOR_OrientisXenotype != null)
        {
            pawn.genes.SetXenotype(BOR_OrientisDefOf.BOR_OrientisXenotype);
        }
        if (!pawn.health.hediffSet.HasHediff(BOR_OrientisDefOf.BOR_Orientis))
        {
            pawn.health.AddHediff(BOR_OrientisDefOf.BOR_Orientis);
        }
        pawn.needs?.AddOrRemoveNeedsAsAppropriate();
        if (pawn.workSettings != null && pawn.workSettings.EverWork && !pawn.WorkTypeIsDisabled(BOR_OrientisDefOf.BOR_Meditate))
        {
            pawn.workSettings.SetPriority(BOR_OrientisDefOf.BOR_Meditate, 3);
        }
    }

    public static void GrantManifest(Pawn pawn)
    {
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        if (pawn.abilities.GetAbility(BOR_OrientisDefOf.BOR_OrientisManifest) == null)
        {
            pawn.abilities.GainAbility(BOR_OrientisDefOf.BOR_OrientisManifest);
        }
    }
}
