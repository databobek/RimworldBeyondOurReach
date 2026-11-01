using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

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
