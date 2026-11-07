using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Marks a building as buildable and usable only by Umbral pawns.
public class DefModExtension_UmbralOnly : DefModExtension
{
}

// Marks Umbral body parts; they cannot be removed by surgery.
public class DefModExtension_UmbralPart : DefModExtension
{
}

// Precepts a requiredPreceptsOnly ideoligion should hold although none of its memes require them; placed on the faction.
public class DefModExtension_ExtraPrecepts : DefModExtension
{
    public List<PreceptDef> precepts = new();
}

public class UmbralPartReplacement
{
    public BodyPartDef bodyPart = null!;
    public HediffDef hediff = null!;
}

// Tunables for the transformation; placed on BOR_UmbralTransformation.
public class DefModExtension_UmbralTransformation : DefModExtension
{
    public float phaseAChance = 0.25f;
    public float phaseBChance = 0.05f;
    public float phaseCChance = 0.2f;

    // Each unit of tenebrite emission around the pawn adds this fraction to the trigger chance.
    public float emissionChanceFactor = 0.5f;

    public List<UmbralPartReplacement> replacements = new();
}

[DefOf]
public static class BOR_UmbralDefOf
{
    public static HediffDef BOR_Umbral = null!;
    public static HediffDef BOR_UmbralTransformation = null!;
    public static HediffDef BOR_UmbralExoskeleton = null!;
    public static HediffDef BOR_UmbralExoskull = null!;
    public static HediffDef BOR_UmbralCamo = null!;
    public static HediffDef BOR_UmbralUtilityPod = null!;
    public static HediffDef BOR_DarkArchotechCommune = null!;
    public static ThoughtDef BOR_UmbralEmpty = null!;
    public static HediffDef BOR_UmbralHerald = null!;
    public static JobDef BOR_UmbralMurder = null!;
    [DefAlias("BOR_Umbral")]
    public static PawnKindDef BOR_Umbral_Kind = null!;
    public static HediffDef BOR_SlaughterBlessing = null!;
    public static HediffDef BOR_SlaughterRestraint = null!;
    public static FactionDef BOR_Umbrals = null!;

    [MayRequireBiotech]
    public static XenotypeDef BOR_UmbralXenotype = null!;

    [MayRequireBiotech]
    public static GeneDef BOR_UmbralNoBodyParts = null!;

    [MayRequireBiotech]
    public static GeneDef BOR_UmbralPsychosisResistance = null!;

    [MayRequireBiotech]
    public static GeneDef BOR_UmbralTransformationGene = null!;

    static BOR_UmbralDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_UmbralDefOf));
    }
}
