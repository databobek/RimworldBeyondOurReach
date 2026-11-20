using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Marks a Coventech mechsuit turret component whose sprite must stay upright. The framework rotates a turret's sprite by
// the same angle it aims with (the mech's facing plus the slot's offset), which is right for a top-down gun but wrong
// for a Graphic_Multi that already has art for each facing, like the Helios rocket pod. Aim is left alone.
public class DefModExtension_MechCompUpright : DefModExtension
{
}

// Only patches when the mechsuit framework is loaded. Draw adds curRotationInt to the angle it is given, so taking it
// off first cancels it.
[HarmonyPatch]
public static class MechComponent_Draw_Upright_Patch
{
    private static MethodBase? target;

    public static bool Prepare() =>
        (target ??= AccessTools.Method("taranchuk_mechsuits.MechComponent:Draw",
            new[] { typeof(Rot4), typeof(Vector3), typeof(Mesh), typeof(float), typeof(bool) })) != null;

    public static MethodBase TargetMethod() => target!;

    public static void Prefix(Thing __instance, ref float angle, float ___curRotationInt)
    {
        if (__instance.def.HasModExtension<DefModExtension_MechCompUpright>())
        {
            angle -= ___curRotationInt;
        }
    }
}
