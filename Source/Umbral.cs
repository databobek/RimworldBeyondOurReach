using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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

public static class Umbral
{
    // Cached per pawn instance because the forbid check asks every tick; cleared when the xenotype or the
    // BOR_Umbral hediff changes. Pawns loaded from a save are new instances, so nothing stale survives a load.
    private static readonly ConditionalWeakTable<Pawn, StrongBox<bool>> umbralCache = new();

    // The BOR_Umbral hediff is the fallback when Biotech (and so the xenotype) is not active.
    public static bool IsUmbral(Pawn pawn)
    {
        if (!umbralCache.TryGetValue(pawn, out StrongBox<bool>? cached))
        {
            cached = new StrongBox<bool>(ComputeIsUmbral(pawn));
            umbralCache.Add(pawn, cached);
        }
        return cached.Value;
    }

    private static bool ComputeIsUmbral(Pawn pawn) =>
        (pawn.genes != null && BOR_UmbralDefOf.BOR_UmbralXenotype != null && pawn.genes.Xenotype == BOR_UmbralDefOf.BOR_UmbralXenotype)
        || HasMarker(pawn);

    public static void InvalidateUmbral(Pawn pawn) => umbralCache.Remove(pawn);

    private static bool HasMarker(Pawn pawn) => pawn.health?.hediffSet?.HasHediff(BOR_UmbralDefOf.BOR_Umbral) ?? false;

    private static bool HasGeneOrMarker(Pawn pawn, GeneDef? gene) =>
        (gene != null && pawn.genes != null && pawn.genes.HasActiveGene(gene)) || HasMarker(pawn);

    public static bool HasPsychosisResistance(Pawn pawn) => HasGeneOrMarker(pawn, BOR_UmbralDefOf.BOR_UmbralPsychosisResistance);

    public static bool BlocksBodyParts(Pawn pawn) => HasGeneOrMarker(pawn, BOR_UmbralDefOf.BOR_UmbralNoBodyParts);

    public static bool Transforms(Pawn pawn) => HasGeneOrMarker(pawn, BOR_UmbralDefOf.BOR_UmbralTransformationGene);

    public static DefModExtension_UmbralTransformation Settings =>
        BOR_UmbralDefOf.BOR_UmbralTransformation.GetModExtension<DefModExtension_UmbralTransformation>() ?? new DefModExtension_UmbralTransformation();

    // Parts the pawn still has to have replaced in phase A.
    public static IEnumerable<(BodyPartRecord part, HediffDef hediff)> PendingReplacements(Pawn pawn)
    {
        HediffSet set = pawn.health.hediffSet;
        foreach (UmbralPartReplacement r in Settings.replacements)
        {
            foreach (BodyPartRecord part in pawn.RaceProps.body.GetPartsWithDef(r.bodyPart))
            {
                if (set.PartIsMissing(part) && part.parent != null && set.PartIsMissing(part.parent))
                {
                    continue;
                }
                if (!set.hediffs.Any(h => h.def == r.hediff && h.Part == part))
                {
                    yield return (part, r.hediff);
                }
            }
        }
    }

    public enum Phase
    {
        A,
        B,
        C,
        Done
    }

    public static Phase CurrentPhase(Pawn pawn)
    {
        if (PendingReplacements(pawn).Any())
        {
            return Phase.A;
        }
        HediffSet set = pawn.health.hediffSet;
        if (!set.HasHediff(BOR_UmbralDefOf.BOR_UmbralExoskeleton) || !set.HasHediff(BOR_UmbralDefOf.BOR_UmbralExoskull))
        {
            return Phase.B;
        }
        return set.HasHediff(BOR_UmbralDefOf.BOR_UmbralUtilityPod) ? Phase.Done : Phase.C;
    }

    public static float TriggerChance(Pawn pawn)
    {
        DefModExtension_UmbralTransformation s = Settings;
        float chance = CurrentPhase(pawn) switch
        {
            Phase.A => s.phaseAChance,
            Phase.B => s.phaseBChance,
            Phase.C => s.phaseCChance,
            _ => 0f
        };
        return chance * (1f + MapComponent_TenebralExposure.EmissionFor(pawn) * s.emissionChanceFactor);
    }

    public static void Notify_KilledHumanlike(Pawn killer)
    {
        if (!killer.RaceProps.Humanlike || !Transforms(killer) || killer.health.hediffSet.HasHediff(BOR_UmbralDefOf.BOR_UmbralTransformation))
        {
            return;
        }
        if (Rand.Chance(TriggerChance(killer)))
        {
            killer.health.AddHediff(BOR_UmbralDefOf.BOR_UmbralTransformation);
        }
    }

    // Grants the next transformation step: an Umbral part, then exoskeleton, exoskull and finally the utility pod.
    public static void CompleteTransformation(Pawn pawn)
    {
        string? granted = null;
        switch (CurrentPhase(pawn))
        {
            case Phase.A:
                if (PendingReplacements(pawn).TryRandomElement(out var pick))
                {
                    if (pick.hediff.hediffClass != null && typeof(Hediff_AddedPart).IsAssignableFrom(pick.hediff.hediffClass))
                    {
                        pawn.health.RestorePart(pick.part);
                    }
                    pawn.health.AddHediff(pick.hediff, pick.part);
                    granted = pick.hediff.label;
                }
                break;
            case Phase.B:
                HediffDef next = pawn.health.hediffSet.HasHediff(BOR_UmbralDefOf.BOR_UmbralExoskeleton)
                    ? BOR_UmbralDefOf.BOR_UmbralExoskull
                    : BOR_UmbralDefOf.BOR_UmbralExoskeleton;
                pawn.health.AddHediff(next);
                granted = next.label;
                break;
            case Phase.C:
                pawn.health.AddHediff(BOR_UmbralDefOf.BOR_UmbralUtilityPod);
                granted = BOR_UmbralDefOf.BOR_UmbralUtilityPod.label;
                break;
        }
        if (granted != null && pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("BOR_UmbralTransformed".Translate(pawn.LabelShortCap, granted), pawn, MessageTypeDefOf.PositiveEvent);
        }
    }

    // Turns a crystallized tenebral pawn into an Umbral, as it leaves the Umbral Transformation Pod.
    public static void MakeUmbral(Pawn pawn)
    {
        Crystallization.ClearAll(pawn);
        List<Hediff> tenebral = pawn.health.hediffSet.hediffs
            .Where(h => h.def.HasModExtension<DefModExtension_TenebralImplant>()
                || h.def == BOR_TenebralDefOf.BOR_TenebralPsychosis
                || h.def == BOR_TenebralDefOf.BOR_TenebralBreakCounter)
            .ToList();
        foreach (Hediff h in tenebral)
        {
            if (h is Hediff_AddedPart && h.Part != null)
            {
                pawn.health.RestorePart(h.Part);
            }
            else if (pawn.health.hediffSet.hediffs.Contains(h))
            {
                pawn.health.RemoveHediff(h);
            }
        }
        if (ModsConfig.BiotechActive && pawn.genes != null && BOR_UmbralDefOf.BOR_UmbralXenotype != null)
        {
            pawn.genes.SetXenotype(BOR_UmbralDefOf.BOR_UmbralXenotype);
        }
        if (!HasMarker(pawn))
        {
            pawn.health.AddHediff(BOR_UmbralDefOf.BOR_Umbral);
        }
        pawn.needs?.AddOrRemoveNeedsAsAppropriate();
    }

    public static void RemoveHediffs(Pawn pawn, HediffDef def)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = hediffs.Count - 1; i >= 0; i--)
        {
            if (hediffs[i].def == def)
            {
                pawn.health.RemoveHediff(hediffs[i]);
            }
        }
    }

    public static bool WearsExoskeleton(Pawn pawn) => pawn.health?.hediffSet?.HasHediff(BOR_UmbralDefOf.BOR_UmbralExoskeleton) ?? false;

    public static bool HasUtilityPod(Pawn pawn) => pawn.health?.hediffSet?.HasHediff(BOR_UmbralDefOf.BOR_UmbralUtilityPod) ?? false;

    public static bool IsExoApparel(ThingDef def) =>
        ExoApparel(BOR_UmbralDefOf.BOR_UmbralExoskeleton).Contains(def) || ExoApparel(BOR_UmbralDefOf.BOR_UmbralExoskull).Contains(def);

    private static List<ThingDef> ExoApparel(HediffDef hediff) =>
        hediff.CompProps<HediffCompProperties_UmbralModeApparel>()?.apparelByMode ?? new List<ThingDef>();

    public static bool IsUtilityWeapon(ThingDef def) =>
        BOR_UmbralDefOf.BOR_UmbralUtilityPod.CompProps<HediffCompProperties_UmbralUtilityPod>()?.weapons.Contains(def) ?? false;
}

// Flu-style progression; at full severity the pawn does not die but gains its next Umbral step.
public class HediffComp_UmbralTransformation : HediffComp
{
    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (parent.Severity + severityAdjustment < parent.def.maxSeverity)
        {
            return;
        }
        severityAdjustment = 0f;
        Pawn pawn = Pawn;
        pawn.health.RemoveHediff(parent);
        Umbral.CompleteTransformation(pawn);
    }
}

public enum ExoskeletonMode
{
    Camo,
    Utility,
    Combat
}

// Keeps one locked apparel per exoskeleton mode on the pawn: index = (int)ExoskeletonMode.
public class HediffCompProperties_UmbralModeApparel : HediffCompProperties
{
    public List<ThingDef> apparelByMode = new();

    // Only the exoskeleton owns the mode and its gizmo; the exoskull follows it.
    public bool ownsMode;

    public HediffCompProperties_UmbralModeApparel()
    {
        compClass = typeof(HediffComp_UmbralModeApparel);
    }
}

public class HediffComp_UmbralModeApparel : HediffComp
{
    private const int CheckInterval = 250;

    // Severity of the exoskeleton hediff per mode; its stages carry the mode's stats.
    private static readonly float[] ModeSeverity = { 1f, 2f, 3f };

    private ExoskeletonMode mode = ExoskeletonMode.Utility;

    public HediffCompProperties_UmbralModeApparel Props => (HediffCompProperties_UmbralModeApparel)props;

    public ExoskeletonMode Mode => Props.ownsMode ? mode : OwnerMode(Pawn);

    private static ExoskeletonMode OwnerMode(Pawn pawn)
    {
        Hediff? exo = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskeleton);
        return exo?.TryGetComp<HediffComp_UmbralModeApparel>()?.mode ?? ExoskeletonMode.Utility;
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        if (Props.ownsMode)
        {
            DropOtherApparel();
            ApplyMode(mode);
        }
        EnsureApparel();
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (Pawn.IsHashIntervalTick(CheckInterval, delta))
        {
            EnsureApparel();
        }
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        foreach (Apparel ap in Pawn.apparel?.WornApparel.Where(a => Props.apparelByMode.Contains(a.def)).ToList() ?? new List<Apparel>())
        {
            Pawn.apparel!.Remove(ap);
            ap.Destroy();
        }
        Umbral.RemoveHediffs(Pawn, BOR_UmbralDefOf.BOR_UmbralCamo);
    }

    private void DropOtherApparel()
    {
        Pawn pawn = Pawn;
        if (pawn.apparel == null)
        {
            return;
        }
        foreach (Apparel ap in pawn.apparel.WornApparel.Where(a => !Umbral.IsExoApparel(a.def)).ToList())
        {
            if (pawn.Spawned)
            {
                pawn.apparel.TryDrop(ap, out _, pawn.PositionHeld, forbid: false);
            }
            else
            {
                pawn.apparel.Remove(ap);
                pawn.inventory?.innerContainer.TryAdd(ap);
            }
        }
    }

    // Swaps the worn exo apparel to the def for the current mode, so the visuals follow the mode.
    public void EnsureApparel()
    {
        Pawn pawn = Pawn;
        if (pawn.apparel == null || pawn.Dead || Props.apparelByMode.Count == 0)
        {
            return;
        }
        ThingDef want = Props.apparelByMode[Mathf.Clamp((int)Mode, 0, Props.apparelByMode.Count - 1)];
        List<Apparel> worn = pawn.apparel.WornApparel.Where(a => Props.apparelByMode.Contains(a.def)).ToList();
        if (worn.Count == 1 && worn[0].def == want)
        {
            return;
        }
        foreach (Apparel ap in worn)
        {
            pawn.apparel.Remove(ap);
            ap.Destroy();
        }
        Apparel apparel = (Apparel)ThingMaker.MakeThing(want);
        pawn.apparel.Wear(apparel, dropReplacedApparel: true, locked: true);
    }

    private void ApplyMode(ExoskeletonMode newMode)
    {
        mode = newMode;
        parent.Severity = ModeSeverity[(int)newMode];
        Pawn pawn = Pawn;
        bool camo = newMode == ExoskeletonMode.Camo;
        bool hasCamo = pawn.health.hediffSet.HasHediff(BOR_UmbralDefOf.BOR_UmbralCamo);
        if (camo && !hasCamo)
        {
            pawn.health.AddHediff(BOR_UmbralDefOf.BOR_UmbralCamo);
        }
        else if (!camo && hasCamo)
        {
            Umbral.RemoveHediffs(pawn, BOR_UmbralDefOf.BOR_UmbralCamo);
        }
        EnsureApparel();
        pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskull)?.TryGetComp<HediffComp_UmbralModeApparel>()?.EnsureApparel();
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (!Props.ownsMode || Pawn.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        ThingDef icon = Props.apparelByMode[Mathf.Clamp((int)mode, 0, Props.apparelByMode.Count - 1)];
        yield return new Command_Action
        {
            defaultLabel = "BOR_ExoMode".Translate(("BOR_ExoMode_" + mode).Translate()),
            defaultDesc = "BOR_ExoModeDesc".Translate(),
            icon = icon.uiIcon,
            action = () =>
            {
                List<FloatMenuOption> options = new();
                foreach (ExoskeletonMode m in System.Enum.GetValues(typeof(ExoskeletonMode)))
                {
                    ExoskeletonMode chosen = m;
                    options.Add(new FloatMenuOption(("BOR_ExoMode_" + chosen).Translate(), () => ApplyMode(chosen)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
    }

    public override string CompLabelInBracketsExtra => ("BOR_ExoMode_" + Mode).Translate();

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref mode, "exoMode", ExoskeletonMode.Utility);
    }
}

// The utility pod renders as a weapon: one ammo-free weapon per mode, re-equipped whenever it goes missing.
public class HediffCompProperties_UmbralUtilityPod : HediffCompProperties
{
    public List<ThingDef> weapons = new();

    public HediffCompProperties_UmbralUtilityPod()
    {
        compClass = typeof(HediffComp_UmbralUtilityPod);
    }
}

public class HediffComp_UmbralUtilityPod : HediffComp
{
    private const int CheckInterval = 120;

    private int mode;

    public HediffCompProperties_UmbralUtilityPod Props => (HediffCompProperties_UmbralUtilityPod)props;

    private ThingDef? Wanted => Props.weapons.Count == 0 ? null : Props.weapons[Mathf.Clamp(mode, 0, Props.weapons.Count - 1)];

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        EnsureWeapon();
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (Pawn.IsHashIntervalTick(CheckInterval, delta))
        {
            EnsureWeapon();
        }
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        ThingWithComps? primary = Pawn.equipment?.Primary;
        if (primary != null && Props.weapons.Contains(primary.def))
        {
            Pawn.equipment!.DestroyEquipment(primary);
        }
    }

    public void EnsureWeapon()
    {
        Pawn pawn = Pawn;
        ThingDef? want = Wanted;
        if (want == null || pawn.equipment == null || pawn.Dead || pawn.Downed)
        {
            return;
        }
        ThingWithComps? primary = pawn.equipment.Primary;
        if (primary?.def == want)
        {
            return;
        }
        if (primary != null)
        {
            if (Props.weapons.Contains(primary.def))
            {
                pawn.equipment.DestroyEquipment(primary);
            }
            else if (pawn.Spawned)
            {
                pawn.equipment.TryDropEquipment(primary, out _, pawn.PositionHeld, forbid: false);
            }
            else
            {
                pawn.equipment.Remove(primary);
                pawn.inventory?.innerContainer.TryAdd(primary);
            }
        }
        pawn.equipment.AddEquipment((ThingWithComps)ThingMaker.MakeThing(want));
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (Pawn.Faction != Faction.OfPlayer || Props.weapons.Count == 0)
        {
            yield break;
        }
        ThingDef current = Wanted!;
        yield return new Command_Action
        {
            defaultLabel = "BOR_UtilityPodMode".Translate(current.LabelCap),
            defaultDesc = "BOR_UtilityPodModeDesc".Translate(),
            icon = current.uiIcon,
            action = () =>
            {
                List<FloatMenuOption> options = new();
                for (int i = 0; i < Props.weapons.Count; i++)
                {
                    int chosen = i;
                    options.Add(new FloatMenuOption(Props.weapons[i].LabelCap, () =>
                    {
                        mode = chosen;
                        EnsureWeapon();
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref mode, "podMode");
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class UmbralPatches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotype))]
    public static class Pawn_GeneTracker_SetXenotype_Patch
    {
        public static void Postfix(Pawn ___pawn) => Umbral.InvalidateUmbral(___pawn);
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.SetXenotypeDirect))]
    public static class Pawn_GeneTracker_SetXenotypeDirect_Patch
    {
        public static void Postfix(Pawn ___pawn) => Umbral.InvalidateUmbral(___pawn);
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult))]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        public static void Postfix(Pawn ___pawn, Hediff hediff)
        {
            if (hediff.def == BOR_UmbralDefOf.BOR_Umbral)
            {
                Umbral.InvalidateUmbral(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    public static class Pawn_HealthTracker_RemoveHediff_Patch
    {
        public static void Postfix(Pawn ___pawn, Hediff hediff)
        {
            if (hediff.def == BOR_UmbralDefOf.BOR_Umbral)
            {
                Umbral.InvalidateUmbral(___pawn);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance.Dead || !__instance.RaceProps.Humanlike || dinfo?.Instigator is not Pawn killer || killer == __instance)
            {
                return;
            }
            Umbral.Notify_KilledHumanlike(killer);
        }
    }

    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
        new[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    public static class EquipmentUtility_CanEquip_Patch
    {
        public static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            if (thing.def.IsApparel && !Umbral.IsExoApparel(thing.def) && Umbral.WearsExoskeleton(pawn))
            {
                cantReason = "BOR_ExoBlocksApparel".Translate();
                __result = false;
            }
            else if (thing.def.IsWeapon && !Umbral.IsUtilityWeapon(thing.def) && Umbral.HasUtilityPod(pawn))
            {
                cantReason = "BOR_PodBlocksWeapon".Translate();
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class ApparelUtility_HasPartsToWear_Patch
    {
        public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (__result && !Umbral.IsExoApparel(apparel) && Umbral.WearsExoskeleton(p))
            {
                __result = false;
            }
        }
    }

    // Umbral-only buildings (and their blueprints and frames) are forbidden to everyone else.
    [HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.IsForbidden), typeof(Thing), typeof(Pawn))]
    public static class ForbidUtility_IsForbidden_Patch
    {
        public static void Postfix(Thing t, Pawn pawn, ref bool __result)
        {
            if (__result || pawn == null || pawn.Faction != Faction.OfPlayer || !pawn.RaceProps.Humanlike)
            {
                return;
            }
            ThingDef def = t.def.entityDefToBuild as ThingDef ?? t.def;
            if (def.modExtensions == null)
            {
                return;
            }
            if (def.HasModExtension<DefModExtension_UmbralOnly>() && !Umbral.IsUmbral(pawn))
            {
                __result = true;
            }
        }
    }
}
