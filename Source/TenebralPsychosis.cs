using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Tunables for the psychosis hediff; placed on BOR_TenebralPsychosis.
public class DefModExtension_TenebralPsychosis : DefModExtension
{
    // Severity floor each tenebral implant holds the pawn at.
    public float floorPerImplant = 0.1f;

    // Tenebrite weapons and armor work like a bionic at this ratio of an implant.
    public float gearImplantRatio = 0.5f;

    // Severity gained per day per unit of nearby tenebrite emission.
    public float exposureGainPerDay = 0.03f;

    // Severity lost per day while above the floor and unexposed.
    public float decayPerDay = 0.05f;

    // Severity gained per day while below the floor.
    public float riseToFloorPerDay = 0.25f;

    // Radius in which tenebrite buildings and floors emit.
    public float emissionRadius = 6f;

    // Emission weight of one building and of one floor cell; the total is capped.
    public float buildingEmission = 1f;
    public float floorEmission = 0.1f;
    public float maxEmission = 3f;

    // Growth multiplier for pawns carrying the psychosis resistance gene.
    public float resistantFactor = 0.25f;
}

// Tunables for the hidden break counter; placed on BOR_TenebralBreakCounter.
public class DefModExtension_TenebralBreaks : DefModExtension
{
    public int holytechThreshold = 3;
    public int umbralThreshold = 6;

    // Parts crystallized per day once the umbral threshold is reached.
    public float crystallizePerDay = 4f;

    // Extra parts crystallized instantly by every break past the umbral threshold.
    public int partsPerExtraBreak = 2;

    // Fraction of the body that has to be crystallized before the Umbral Transformation Pod accepts the pawn.
    public float podThreshold = 0.5f;
}

[DefOf]
public static class BOR_TenebralDefOf
{
    public static ThingDef BOR_Tenebrite = null!;
    public static ThingDef BOR_ComponentIII = null!;
    public static ThingDef BOR_Femtoclusters = null!;
    public static ThingDef BOR_TenebralOrgan = null!;
    public static ThingDef BOR_TenebralAppendage = null!;

    public static HediffDef BOR_TenebralPsychosis = null!;
    public static HediffDef BOR_TenebralBreakCounter = null!;
    public static HediffDef BOR_Crystallized = null!;
    public static HediffDef BOR_HolytechCandidate = null!;

    public static JobDef BOR_AltarUnlock = null!;
    public static JobDef BOR_AltarProduce = null!;
    public static JobDef BOR_LoadTenebralPod = null!;

    static BOR_TenebralDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_TenebralDefOf));
    }
}

public static class TenebralPsychosis
{
    public static DefModExtension_TenebralPsychosis Settings =>
        BOR_TenebralDefOf.BOR_TenebralPsychosis.GetModExtension<DefModExtension_TenebralPsychosis>() ?? new DefModExtension_TenebralPsychosis();

    public static DefModExtension_TenebralBreaks BreakSettings =>
        BOR_TenebralDefOf.BOR_TenebralBreakCounter.GetModExtension<DefModExtension_TenebralBreaks>() ?? new DefModExtension_TenebralBreaks();

    // A pawn is psychotic once the psychosis hediff has grown past its hidden latent stage.
    public static bool IsPsychotic(Pawn pawn)
    {
        Hediff? hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(BOR_TenebralDefOf.BOR_TenebralPsychosis);
        return hediff != null && hediff.Visible;
    }

    public static int Breaks(Pawn pawn) =>
        (pawn.health?.hediffSet?.GetFirstHediffOfDef(BOR_TenebralDefOf.BOR_TenebralBreakCounter) as Hediff_TenebralBreaks)?.breaks ?? 0;

    public static bool CanTakeHolytechExit(Pawn pawn) => Breaks(pawn) >= BreakSettings.holytechThreshold;

    // Everything made with tenebrite: stuff, or tenebrite in the cost list.
    public static bool MadeOfTenebrite(Thing thing) =>
        thing.Stuff == BOR_TenebralDefOf.BOR_Tenebrite || TenebriteDefs.Contains(thing.def);

    private static HashSet<ThingDef>? tenebriteDefs;
    private static HashSet<TerrainDef>? tenebriteTerrain;

    private static HashSet<ThingDef> TenebriteDefs => tenebriteDefs ??= DefDatabase<ThingDef>.AllDefs
        .Where(d => d.costList != null && d.costList.Any(c => c.thingDef == BOR_TenebralDefOf.BOR_Tenebrite))
        .ToHashSet();

    private static HashSet<TerrainDef> TenebriteTerrain => tenebriteTerrain ??= DefDatabase<TerrainDef>.AllDefs
        .Where(d => d.costList != null && d.costList.Any(c => c.thingDef == BOR_TenebralDefOf.BOR_Tenebrite))
        .ToHashSet();

    // Worn or held tenebrite gear, counted as implants at the gear ratio.
    public static float GearImplants(Pawn pawn)
    {
        int count = 0;
        if (pawn.apparel != null)
        {
            foreach (Apparel apparel in pawn.apparel.WornApparel)
            {
                if (MadeOfTenebrite(apparel))
                {
                    count++;
                }
            }
        }
        if (pawn.equipment != null)
        {
            foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading)
            {
                if (MadeOfTenebrite(eq))
                {
                    count++;
                }
            }
        }
        return count * Settings.gearImplantRatio;
    }

    // Emission from tenebrite buildings, walls and floors around the pawn, like pollution.
    public static float Emission(Pawn pawn)
    {
        if (!pawn.Spawned)
        {
            return 0f;
        }
        DefModExtension_TenebralPsychosis s = Settings;
        Map map = pawn.Map;
        float total = 0f;
        int cells = GenRadial.NumCellsInRadius(s.emissionRadius);
        for (int i = 0; i < cells; i++)
        {
            IntVec3 c = pawn.Position + GenRadial.RadialPattern[i];
            if (!c.InBounds(map))
            {
                continue;
            }
            if (TenebriteTerrain.Contains(c.GetTerrain(map)))
            {
                total += s.floorEmission;
            }
            List<Thing> things = map.thingGrid.ThingsListAtFast(c);
            for (int j = 0; j < things.Count; j++)
            {
                Thing t = things[j];
                // Multi-cell buildings count once, at their origin.
                if (t.def.category == ThingCategory.Building && t.Position == c && MadeOfTenebrite(t))
                {
                    total += s.buildingEmission;
                }
            }
        }
        return Mathf.Min(total, s.maxEmission);
    }

    public static float ResistanceFactor(Pawn pawn) =>
        Umbral.HasPsychosisResistance(pawn) ? Settings.resistantFactor : 1f;

    // Moves the pawn's psychosis by one exposure check worth `days`.
    public static void UpdatePawn(Pawn pawn, float days, float emission)
    {
        DefModExtension_TenebralPsychosis s = Settings;
        float resist = ResistanceFactor(pawn);
        float floor = Mathf.Min(0.95f, (TenebralMurder.ImplantCount(pawn) + GearImplants(pawn)) * s.floorPerImplant) * resist;
        HediffSet set = pawn.health.hediffSet;
        Hediff? hediff = set.GetFirstHediffOfDef(BOR_TenebralDefOf.BOR_TenebralPsychosis);
        if (hediff == null)
        {
            if (floor <= 0f && emission <= 0f)
            {
                return;
            }
            hediff = HediffMaker.MakeHediff(BOR_TenebralDefOf.BOR_TenebralPsychosis, pawn);
            hediff.Severity = 0.001f;
            pawn.health.AddHediff(hediff);
        }
        float sev = hediff.Severity;
        if (emission > 0f)
        {
            sev += emission * s.exposureGainPerDay * days * resist;
        }
        if (sev < floor)
        {
            sev = Mathf.Min(floor, sev + s.riseToFloorPerDay * days);
        }
        else if (emission <= 0f && sev > floor)
        {
            sev = Mathf.Max(floor, sev - s.decayPerDay * days);
        }
        if (sev <= 0.001f && floor <= 0f && emission <= 0f)
        {
            pawn.health.RemoveHediff(hediff);
            return;
        }
        hediff.Severity = Mathf.Min(1f, sev);
    }
}

// Runs the tenebrite exposure checks for every humanlike on the map, a few pawns per tick.
public class MapComponent_TenebralExposure : MapComponent
{
    private const int Interval = 2000;

    private readonly Dictionary<Pawn, float> lastEmission = new();

    public MapComponent_TenebralExposure(Map map) : base(map)
    {
    }

    public float LastEmission(Pawn pawn) => lastEmission.TryGetValue(pawn, out float e) ? e : 0f;

    public static float EmissionFor(Pawn pawn) =>
        pawn.MapHeld?.GetComponent<MapComponent_TenebralExposure>()?.LastEmission(pawn) ?? 0f;

    public override void MapComponentTick()
    {
        int tick = Find.TickManager.TicksGame;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllHumanlikeSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn.Dead || (tick + pawn.thingIDNumber) % Interval != 0)
            {
                continue;
            }
            float emission = TenebralPsychosis.Emission(pawn);
            lastEmission[pawn] = emission;
            TenebralPsychosis.UpdatePawn(pawn, (float)Interval / GenDate.TicksPerDay, emission);
        }
        if (tick % GenDate.TicksPerDay == 0)
        {
            lastEmission.RemoveAll(kv => kv.Key.Destroyed || kv.Key.MapHeld != map);
        }
    }
}

// Hidden counter of mental breaks suffered while psychotic. Also drives crystallization past the umbral threshold.
public class Hediff_TenebralBreaks : HediffWithComps
{
    private const int CrystalInterval = GenDate.TicksPerHour;

    public int breaks;

    public override bool Visible => false;

    public void Notify_Break()
    {
        breaks++;
        DefModExtension_TenebralBreaks s = TenebralPsychosis.BreakSettings;
        if (pawn.Faction == Faction.OfPlayer && breaks == s.holytechThreshold)
        {
            Find.LetterStack.ReceiveLetter("BOR_HolytechExitLabel".Translate(pawn.LabelShortCap),
                "BOR_HolytechExitText".Translate(pawn.Named("PAWN")), LetterDefOf.NeutralEvent, pawn);
        }
        if (breaks == s.umbralThreshold)
        {
            if (pawn.Faction == Faction.OfPlayer)
            {
                Find.LetterStack.ReceiveLetter("BOR_CrystallizationLabel".Translate(pawn.LabelShortCap),
                    "BOR_CrystallizationText".Translate(pawn.Named("PAWN")), LetterDefOf.ThreatSmall, pawn);
            }
        }
        else if (breaks > s.umbralThreshold)
        {
            for (int i = 0; i < s.partsPerExtraBreak; i++)
            {
                Crystallization.CrystallizeRandomPart(pawn);
            }
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (!pawn.IsHashIntervalTick(CrystalInterval, delta))
        {
            return;
        }
        DefModExtension_TenebralBreaks s = TenebralPsychosis.BreakSettings;
        if (breaks < s.umbralThreshold || pawn.InContainerEnclosed)
        {
            return;
        }
        // Crystallization keeps going once it has begun, psychotic or not.
        if (Rand.Chance(s.crystallizePerDay * CrystalInterval / GenDate.TicksPerDay))
        {
            Crystallization.CrystallizeRandomPart(pawn);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref breaks, "breaks");
    }
}

public static class Crystallization
{
    // Heart, brain and every other vital part never crystallize, nor does the core part. Eating parts are spared too,
    // or the pawn would starve before reaching the pod.
    public static bool Eligible(Pawn pawn, BodyPartRecord part)
    {
        if (part == pawn.RaceProps.body.corePart || pawn.health.hediffSet.PartIsMissing(part))
        {
            return false;
        }
        List<BodyPartTagDef> tags = part.def.tags;
        for (int i = 0; i < tags.Count; i++)
        {
            if (tags[i].vital || tags[i] == BodyPartTagDefOf.EatingSource || tags[i] == BodyPartTagDefOf.EatingPathway)
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsCrystallized(Pawn pawn, BodyPartRecord? part)
    {
        if (part == null)
        {
            return false;
        }
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def == BOR_TenebralDefOf.BOR_Crystallized && hediffs[i].Part == part)
            {
                return true;
            }
        }
        return false;
    }

    public static float Fraction(Pawn pawn)
    {
        int total = 0;
        int crystal = 0;
        foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
        {
            if (!Eligible(pawn, part))
            {
                continue;
            }
            total++;
            if (IsCrystallized(pawn, part))
            {
                crystal++;
            }
        }
        return total == 0 ? 0f : (float)crystal / total;
    }

    public static bool CrystallizeRandomPart(Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike || pawn.Dead)
        {
            return false;
        }
        if (!pawn.RaceProps.body.AllParts.Where(p => Eligible(pawn, p) && !IsCrystallized(pawn, p)).TryRandomElement(out BodyPartRecord part))
        {
            return false;
        }
        pawn.health.AddHediff(BOR_TenebralDefOf.BOR_Crystallized, part);
        return true;
    }

    public static void ClearAll(Pawn pawn)
    {
        List<Hediff> crystals = pawn.health.hediffSet.hediffs.Where(h => h.def == BOR_TenebralDefOf.BOR_Crystallized).ToList();
        foreach (Hediff h in crystals)
        {
            pawn.health.RemoveHediff(h);
        }
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class TenebralPsychosisPatches
{
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public static class MentalStateHandler_TryStartMentalState_Patch
    {
        public static void Postfix(Pawn ___pawn, bool __result, bool causedByDamage, bool causedByPsycast)
        {
            if (!__result || causedByDamage || causedByPsycast || !___pawn.RaceProps.Humanlike || !TenebralPsychosis.IsPsychotic(___pawn))
            {
                return;
            }
            if (___pawn.health.hediffSet.GetFirstHediffOfDef(BOR_TenebralDefOf.BOR_TenebralBreakCounter) is not Hediff_TenebralBreaks counter)
            {
                counter = (Hediff_TenebralBreaks)HediffMaker.MakeHediff(BOR_TenebralDefOf.BOR_TenebralBreakCounter, ___pawn);
                ___pawn.health.AddHediff(counter);
            }
            counter.Notify_Break();
        }
    }
}
