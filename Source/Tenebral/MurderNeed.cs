using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Marker placed on the abstract tenebral implant and bionic part hediff bases; every child counts towards the murder need.
public class DefModExtension_TenebralImplant : DefModExtension
{
}

public static class TenebralMurder
{
    public const float BaseDays = 15f;
    public const float MinDays = 1f;

    public static int ImplantCount(Pawn pawn)
    {
        int count = 0;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def.HasModExtension<DefModExtension_TenebralImplant>())
            {
                count++;
            }
        }
        return count;
    }

    public static float DaysToEmpty(int implants) =>
        implants <= 0 ? BaseDays : System.Math.Max(MinDays, BaseDays / implants);
}

public class Need_Murder : Need
{
    private const int Interval = 150;

    public Need_Murder(Pawn pawn) : base(pawn)
    {
    }

    public override int GUIChangeArrow => -1;

    public bool Unfilled => CurLevel <= 0.001f;

    public override void SetInitialLevel() => CurLevel = 1f;

    public override void NeedInterval()
    {
        if (IsFrozen)
        {
            return;
        }
        int implants = TenebralMurder.ImplantCount(pawn);
        CurLevel -= Interval / (TenebralMurder.DaysToEmpty(implants) * GenDate.TicksPerDay);
    }

    public void Notify_Killed() => CurLevel = MaxLevel;
}

public class ThoughtWorker_Murder : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        Need_Murder? need = p.needs?.TryGetNeed<Need_Murder>();
        return need != null && need.Unfilled;
    }
}

[StaticConstructorOnStartup]
public static class TenebralPainStripper
{
    static TenebralPainStripper()
    {
        foreach (HediffDef def in DefDatabase<HediffDef>.AllDefs)
        {
            if (def.stages == null || !def.HasModExtension<DefModExtension_TenebralImplant>())
            {
                continue;
            }
            foreach (HediffStage stage in def.stages)
            {
                stage.painOffset = 0f;
            }
        }
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class MurderPatches
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Pawn_NeedsTracker_ShouldHaveNeed_Patch
    {
        public static void Postfix(Pawn ___pawn, NeedDef nd, ref bool __result)
        {
            if (nd != BOR_MurderDefOf.BOR_Murder)
            {
                return;
            }
            if (__result && TenebralMurder.ImplantCount(___pawn) == 0)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.Notify_HediffChanged))]
    public static class Pawn_HealthTracker_Notify_HediffChanged_Patch
    {
        public static void Postfix(Pawn ___pawn, Hediff? hediff)
        {
            if (hediff == null || hediff.def.HasModExtension<DefModExtension_TenebralImplant>())
            {
                ___pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
        typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult))]
    public static class Pawn_HealthTracker_AddHediff_Patch
    {
        public static void Postfix(Pawn ___pawn, Hediff hediff)
        {
            if (hediff.def.HasModExtension<DefModExtension_TenebralImplant>())
            {
                ___pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.RemoveHediff))]
    public static class Pawn_HealthTracker_RemoveHediff_Patch
    {
        public static void Postfix(Pawn ___pawn, Hediff hediff)
        {
            if (hediff.def.HasModExtension<DefModExtension_TenebralImplant>())
            {
                ___pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        public static void Prefix(Pawn __instance, DamageInfo? dinfo, out bool __state)
        {
            __state = !__instance.Dead && __instance.RaceProps.Humanlike;
            if (!__state || dinfo?.Instigator is not Pawn killer || killer == __instance)
            {
                return;
            }
            killer.needs?.TryGetNeed<Need_Murder>()?.Notify_Killed();
        }
    }

    [HarmonyPatch(typeof(MentalBreaker), nameof(MentalBreaker.TryDoRandomMoodCausedMentalBreak))]
    public static class MentalBreaker_Patch
    {
        public static bool Prefix(MentalBreaker __instance, Pawn ___pawn, ref bool __result)
        {
            Need_Murder? need = ___pawn.needs?.TryGetNeed<Need_Murder>();
            if (need == null || !need.Unfilled)
            {
                return true;
            }
            if (!__instance.CanDoRandomMentalBreaks || ___pawn.Downed || !___pawn.Awake() || ___pawn.InAggroMentalState)
            {
                __result = false;
                return false;
            }

            int implants = TenebralMurder.ImplantCount(___pawn);
            bool defect = Rand.Chance(implants * 0.01f);
            Faction? oldFaction = ___pawn.Faction;
            bool started = ___pawn.mindState.mentalStateHandler.TryStartMentalState(
                MentalStateDefOf.Berserk, "BOR_MurderBreakReason".Translate(), forceWake: true);
            __result = started;
            if (started && defect && oldFaction == Faction.OfPlayer)
            {
                Faction? hostile = Find.FactionManager.RandomEnemyFaction(allowHidden: true, allowDefeated: true,
                    allowNonHumanlike: false) ?? Faction.OfAncientsHostile;
                ___pawn.SetFaction(hostile);
                Find.LetterStack.ReceiveLetter("BOR_MurderDefectLabel".Translate(___pawn.LabelShortCap),
                    "BOR_MurderDefectText".Translate(___pawn.LabelShortCap), LetterDefOf.NegativeEvent, ___pawn);
            }
            return false;
        }
    }
}

[DefOf]
public static class BOR_MurderDefOf
{
    public static NeedDef BOR_Murder = null!;

    static BOR_MurderDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_MurderDefOf));
    }
}
