using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace BeyondOurReach;

public static class Umbral
{
    public const float FixedMood = 0.5f;

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
        if (granted == null)
        {
            return;
        }
        if (pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("BOR_UmbralTransformed".Translate(pawn.LabelShortCap, granted), pawn, MessageTypeDefOf.PositiveEvent);
        }
        DarkAbilities.RollUmbralTransformation(pawn);
    }

    // Turns a crystallized tenebral pawn into an Umbral, as it leaves a Tenebral pod in transformation mode.
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
        Hollow(pawn);
        AdoptUmbralIdeo(pawn);
    }

    // Umbrals feel nothing: traits and memories go here, and UmbralPatches blocks every new thought and fixes the mood.
    private static void Hollow(Pawn pawn)
    {
        if (pawn.story?.traits != null)
        {
            foreach (Trait trait in pawn.story.traits.allTraits.ToList())
            {
                pawn.story.traits.RemoveTrait(trait);
            }
        }
        Need_Mood? mood = pawn.needs?.mood;
        if (mood != null)
        {
            mood.thoughts.memories.Memories.Clear();
            mood.thoughts.situational.Notify_SituationalThoughtsDirty();
            mood.CurLevel = FixedMood;
        }
    }

    // A colonist who turns Umbral takes up the Umbral ideoligion; the rest of the colony keeps theirs.
    private static void AdoptUmbralIdeo(Pawn pawn)
    {
        if (!ModsConfig.IdeologyActive || pawn.ideo == null || pawn.Faction != Faction.OfPlayer)
        {
            return;
        }
        Ideo? ideo = Find.FactionManager.FirstFactionOfDef(BOR_UmbralDefOf.BOR_Umbrals)?.ideos?.PrimaryIdeo;
        if (ideo == null || pawn.Ideo == ideo)
        {
            return;
        }
        // Spawning reveals a hidden ideoligion, but this pawn may already be spawned.
        ideo.hidden = false;
        pawn.ideo.SetIdeo(ideo);
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

// The one thought an Umbral has. It changes nothing; the mood is fixed elsewhere.
public class ThoughtWorker_UmbralEmpty : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p) =>
        Umbral.IsUmbral(p) ? ThoughtState.ActiveDefault : ThoughtState.Inactive;
}

// Flu-style progression; at full severity the pawn does not die but gains its next Umbral step.
public class HediffComp_UmbralTransformation : HediffComp
{
    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        // The severity comp runs first and only adds progress on some ticks.
        if (severityAdjustment > 0f && RighteousSlaughter.IsBlessed(Pawn))
        {
            severityAdjustment *= RighteousSlaughter.Settings.transformationSpeed;
        }
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
