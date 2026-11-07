using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class UmbralPatches
{
    // Vanilla picks any non-archite gene from a pawn it accepts, so these pawns are refused outright.
    [HarmonyPatch(typeof(Building_GeneExtractor), nameof(Building_GeneExtractor.CanAcceptPawn))]
    public static class Building_GeneExtractor_CanAcceptPawn_Patch
    {
        public static void Postfix(Pawn pawn, ref AcceptanceReport __result)
        {
            if (__result.Accepted && (Umbral.IsUmbral(pawn) || Orientis.IsOrientis(pawn)))
            {
                __result = "BOR_GenesNotExtractable".Translate(pawn.LabelShortCap);
            }
        }
    }

    // requiredPreceptsOnly skips the code that fills tmpMemes, so vanilla reads the previous faction's memes here and
    // can add their rituals, ignoring disallowedPrecepts. The next ideoligion fills the list again.
    [HarmonyPatch(typeof(IdeoFoundation), nameof(IdeoFoundation.InitPrecepts))]
    public static class IdeoFoundation_InitPrecepts_Patch
    {
        public static void Prefix(IdeoFoundation __instance, IdeoGenerationParms parms, List<MemeDef> ___tmpMemes)
        {
            if (parms.forFaction != BOR_UmbralDefOf.BOR_Umbrals)
            {
                return;
            }
            ___tmpMemes.Clear();
            foreach (PreceptDef precept in parms.forFaction.GetModExtension<DefModExtension_ExtraPrecepts>()?.precepts ?? new List<PreceptDef>())
            {
                __instance.ideo.AddPrecept(PreceptMaker.MakePrecept(precept));
            }
        }
    }

    // Every cooldown goes through here, so the Righteous Slaughter's blessing shortens them all.
    [HarmonyPatch(typeof(Ability), nameof(Ability.StartCooldown))]
    public static class Ability_StartCooldown_Patch
    {
        public static void Prefix(Ability __instance, ref int ticks)
        {
            if (RighteousSlaughter.IsBlessed(__instance.pawn))
            {
                ticks = (int)(ticks / RighteousSlaughter.Settings.cooldownSpeed);
            }
        }
    }

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
            if (thing.def.IsApparel && Umbral.WearsExoskeleton(pawn) && !Umbral.IsExoApparel(thing.def))
            {
                cantReason = "BOR_ExoBlocksApparel".Translate();
                __result = false;
            }
            else if (thing.def.IsWeapon && Umbral.HasUtilityPod(pawn) && !Umbral.IsUtilityWeapon(thing.def))
            {
                cantReason = "BOR_PodBlocksWeapon".Translate();
                __result = false;
            }
        }
    }

    // Situational thoughts and new memories, social ones included, all ask this first.
    [HarmonyPatch(typeof(ThoughtUtility), nameof(ThoughtUtility.CanGetThought))]
    public static class ThoughtUtility_CanGetThought_Patch
    {
        public static void Postfix(Pawn pawn, ThoughtDef def, ref bool __result)
        {
            if (__result && def != BOR_UmbralDefOf.BOR_UmbralEmpty && Umbral.IsUmbral(pawn))
            {
                __result = false;
            }
        }
    }

    // Replaces the result outright, so the difficulty's mood offset doesn't move it either.
    [HarmonyPatch(typeof(Need_Mood), nameof(Need_Mood.CurInstantLevel), MethodType.Getter)]
    public static class Need_Mood_CurInstantLevel_Patch
    {
        public static void Postfix(Pawn ___pawn, ref float __result)
        {
            if (Umbral.IsUmbral(___pawn))
            {
                __result = Umbral.FixedMood;
            }
        }
    }

    // Recruiting and enslaving unlock everything a pawn wears. The exoskeleton destroys on drop, which vanilla's
    // RemoveApparel job can't handle, and it can't come off anyway. Lock and Unlock read IsLocked, so it can't just lie.
    [HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.UnlockAll))]
    public static class Pawn_ApparelTracker_UnlockAll_Patch
    {
        public static void Postfix(Pawn_ApparelTracker __instance)
        {
            foreach (Apparel apparel in __instance.WornApparel)
            {
                if (Umbral.IsExoApparel(apparel.def))
                {
                    __instance.Lock(apparel);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ApparelUtility), nameof(ApparelUtility.HasPartsToWear))]
    public static class ApparelUtility_HasPartsToWear_Patch
    {
        public static void Postfix(Pawn p, ThingDef apparel, ref bool __result)
        {
            if (__result && Umbral.WearsExoskeleton(p) && !Umbral.IsExoApparel(apparel))
            {
                __result = false;
            }
        }
    }

    // Umbral-only buildings (and their blueprints and frames) are forbidden to everyone else. After the Path of Light,
    // any tenebral or Umbral building that is left is forbidden to all, but can still be taken down.
    // Called from the one IsForbidden postfix, in OrientisRestrictions, which has already checked the pawn.
    public static bool ForbidsUmbralBuilding(Thing t, Pawn pawn)
    {
        ThingDef def = t.def.entityDefToBuild as ThingDef ?? t.def;
        if (def.modExtensions == null)
        {
            return false;
        }
        if (def.HasModExtension<DefModExtension_UmbralOnly>() && !Umbral.IsUmbral(pawn))
        {
            return true;
        }
        return DarkBuildings.Banned(def) && t.MapHeld?.designationManager.DesignationOn(t) == null;
    }

    // Tenebral and Umbral buildings leave the build menu for good after the Path of Light.
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Visible), MethodType.Getter)]
    public static class Designator_Build_Visible_Patch
    {
        public static void Postfix(Designator_Build __instance, ref bool __result)
        {
            if (__result && DarkBuildings.Banned(__instance.PlacingDef))
            {
                __result = false;
            }
        }
    }
}
