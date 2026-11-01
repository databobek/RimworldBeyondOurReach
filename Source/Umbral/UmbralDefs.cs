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
