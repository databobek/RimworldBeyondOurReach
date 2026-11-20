using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// MuzzleFlash places a pawn's flash at its weapon's cached draw position, which only DrawEquipmentAiming fills. A
// mechsuit turret's gun is never drawn that way (the framework draws the arm itself), so the cache stays empty and
// the flash would sit at the map origin. This feeds it the arm's draw position whenever the framework draws the arm;
// MuzzleFlash's own offsets then push the flash out along the aim to the barrel tip.
// Only patches when both the mechsuit framework and MuzzleFlash are loaded; everything is looked up by name.
[HarmonyPatch]
public static class MechComponent_Draw_MuzzleFlash_Patch
{
    private static MethodBase? target;
    private static FieldInfo? turretField;
    private static FieldInfo? gunField;
    private static MethodInfo? getDrawPos;
    private static Action<Thing, Vector3>? setDrawPos;

    public static bool Prepare()
    {
        if (target != null)
        {
            return true;
        }
        Type? component = AccessTools.TypeByName("taranchuk_mechsuits.MechComponent");
        Type? turret = AccessTools.TypeByName("taranchuk_mechsuits.MechCompTurret");
        Type? cache = AccessTools.TypeByName("MuzzleFlash.WeaponPositionCache");
        if (component == null || turret == null || cache == null)
        {
            return false;
        }
        MethodInfo? set = AccessTools.Method(cache, "SetDrawPos", new[] { typeof(Thing), typeof(Vector3) });
        turretField = AccessTools.Field(component, "turret");
        gunField = AccessTools.Field(turret, "gun");
        getDrawPos = AccessTools.Method(component, "GetDrawPos", new[] { typeof(bool) });
        target = AccessTools.Method(component, "Draw", new[] { typeof(Rot4), typeof(Vector3), typeof(Mesh), typeof(float), typeof(bool) });
        if (set == null || turretField == null || gunField == null || getDrawPos == null || target == null)
        {
            target = null;
            return false;
        }
        setDrawPos = (Action<Thing, Vector3>)Delegate.CreateDelegate(typeof(Action<Thing, Vector3>), set);
        return true;
    }

    public static MethodBase TargetMethod() => target!;

    public static void Postfix(Thing __instance)
    {
        if (turretField!.GetValue(__instance) is not { } turret || gunField!.GetValue(turret) is not Thing gun)
        {
            return;
        }
        setDrawPos!(gun, (Vector3)getDrawPos!.Invoke(__instance, new object[] { false }));
    }
}
