using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// A BOR tier: on a faction, how advanced its drop pods are; on an anti-air turret, how advanced the turret is.
// Unmarked factions and turrets (vanilla, CE, VFE Security) are tier 0, the Ultra level.
public class DefModExtension_BorTier : DefModExtension
{
    public int tier;
}

// Marks a skyfaller that anti-air must leave alone: the guests the strange signal chains send. The tenebral visitor is
// hostile by design and lands by drop pod, so anti-air would shoot it, and the chain's reward with it, out of the sky.
public class DefModExtension_NoAntiAir : DefModExtension
{
    public static bool On(Thing? thing) => thing?.def.HasModExtension<DefModExtension_NoAntiAir>() ?? false;
}

// Who may shoot down what. Turrets shoot at every hostile pod as before, but a hit only counts on drop pods of factions
// up to one tier above the turret's own: Ultra-level anti-air downs up to Solar, Solar up to Kwazaari, Kwazaari up to
// Tenebral, Tenebral up to Umbral. Anything harder just deflects the shot. The signal guests are never shot at.
public static class AntiAir
{
    private const int DeflectTextInterval = GenTicks.TicksPerRealSecond;

    private static readonly List<Thing> contents = new();

    private static readonly Dictionary<int, int> lastDeflectText = new();

    public static int Tier(Def? def) => def?.GetModExtension<DefModExtension_BorTier>()?.tier ?? 0;

    // The most advanced faction among the pawns on board, however deep they sit (a transporter inside a skyfaller).
    public static int PodTier(Thing skyfaller)
    {
        if (skyfaller is not IThingHolder holder)
        {
            return 0;
        }
        contents.Clear();
        ThingOwnerUtility.GetAllThingsRecursively(holder, contents, false);
        int tier = contents.OfType<Pawn>().Select(p => Tier(p.Faction?.def)).DefaultIfEmpty(0).Max();
        contents.Clear();
        return tier;
    }

    public static bool CanDown(Thing? turret, Thing skyfaller) => PodTier(skyfaller) <= Tier(turret?.def) + 1;

    // A spark and, at most once a second per pod, a floating "Deflected".
    public static void Deflect(Thing skyfaller)
    {
        Map? map = skyfaller.MapHeld;
        if (map == null)
        {
            return;
        }
        FleckMaker.ThrowMicroSparks(skyfaller.DrawPos, map);
        int now = Find.TickManager.TicksGame;
        if (lastDeflectText.TryGetValue(skyfaller.thingIDNumber, out int last) && now - last < DeflectTextInterval)
        {
            return;
        }
        if (lastDeflectText.Count > 64)
        {
            lastDeflectText.Clear();
        }
        lastDeflectText[skyfaller.thingIDNumber] = now;
        MoteMaker.ThrowText(skyfaller.DrawPos, map, "BOR_AntiAir_Deflected".Translate(), UnityEngine.Color.white);
    }
}

// Combat Extended: CIWS turrets that target skyfallers directly. Friendly means "do not shoot"; only the guests are.
[HarmonyPatch]
public static class CE_VerbCIWSSkyfaller_AntiAir_Patch
{
    private static MethodBase? target;

    public static bool Prepare() => (target ??= AccessTools.Method("CombatExtended.VerbCIWSSkyfaller:IsFriendlyTo")) != null;

    public static MethodBase TargetMethod() => target!;

    public static void Postfix(Thing __0, ref bool __result)
    {
        if (DefModExtension_NoAntiAir.On(__0))
        {
            __result = true;
        }
    }
}

// Combat Extended: CIWS turrets that target skyfallers through their CIWS target comp.
[HarmonyPatch]
public static class CE_CompCIWSTargetSkyfaller_AntiAir_Patch
{
    private static MethodBase? target;

    public static bool Prepare() => (target ??= AccessTools.Method("CombatExtended.CompCIWSTarget_Skyfaller:IsFriendlyTo")) != null;

    public static MethodBase TargetMethod() => target!;

    public static void Postfix(ThingComp __instance, ref bool __result)
    {
        if (DefModExtension_NoAntiAir.On(__instance.parent))
        {
            __result = true;
        }
    }
}

// Combat Extended: a CIWS round hitting a skyfaller. Out of the turret's tier, the round is spent but does nothing.
[HarmonyPatch]
public static class CE_CIWSImpactSkyfaller_AntiAir_Patch
{
    private static MethodBase? target;
    private static FieldInfo? launcher;

    public static bool Prepare()
    {
        // Field("Type:name") throws when the type is missing (no CE), so look the type up first.
        target ??= AccessTools.Method("CombatExtended.CompCIWSImpactHandler_Skyfaller:OnImpact");
        if (target == null)
        {
            return false;
        }
        launcher ??= AccessTools.Field(AccessTools.TypeByName("CombatExtended.ProjectileCE"), "launcher");
        return launcher != null;
    }

    public static MethodBase TargetMethod() => target!;

    public static bool Prefix(ThingComp __instance, object __0)
    {
        if (AntiAir.CanDown(launcher!.GetValue(__0) as Thing, __instance.parent))
        {
            return true;
        }
        AntiAir.Deflect(__instance.parent);
        return false;
    }
}

// VFE Security: point-defense turrets that shoot down drop pods with hostile pawns inside. They never aim at the guests.
[HarmonyPatch]
public static class VFES_PointDefense_AntiAir_Patch
{
    private static MethodBase? target;

    public static bool Prepare() => (target ??= AccessTools.Method("VFESecurity.CompPointDefense:IsValidTransporter")) != null;

    public static MethodBase TargetMethod() => target!;

    public static void Postfix(Thing __0, ref bool __result)
    {
        if (__result && DefModExtension_NoAntiAir.On(__0))
        {
            __result = false;
        }
    }
}

// VFE Security: the shot is already fired (fuel spent, flash shown); out of the turret's tier it deflects off the pod.
[HarmonyPatch]
public static class VFES_PointDefenseIntercept_AntiAir_Patch
{
    private static MethodBase? target;

    public static bool Prepare() => (target ??= AccessTools.Method("VFESecurity.CompPointDefense:TryIntercept")) != null;

    public static MethodBase TargetMethod() => target!;

    public static bool Prefix(ThingComp __instance, Thing __0)
    {
        if (__0 is not Skyfaller || AntiAir.CanDown(__instance.parent, __0))
        {
            return true;
        }
        AntiAir.Deflect(__0);
        return false;
    }
}
