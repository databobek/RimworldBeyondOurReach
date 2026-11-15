using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class KwazaariFireMode
{
    public string labelKey = "";
    public string descKey = "";
    public string iconPath = "";
    public ThingDef? projectile;

    // Cartridges one shot uses. 10 means a magazine lasts a tenth as many shots.
    public int ammoPerShot = 1;

    [Unsaved] private Texture2D? icon;

    public Texture2D Icon => icon ??= ContentFinder<Texture2D>.Get(iconPath, false) ?? BaseContent.BadTex;
}

// Kwazaari gauss weapons take one cartridge and program its core per shot. The selected mode decides which
// projectile the cartridge fires. Only the Combat Extended patch adds this comp, since only CE has ammo.
public class CompProperties_KwazaariFireMode : CompProperties
{
    public List<KwazaariFireMode> modes = new();

    public CompProperties_KwazaariFireMode()
    {
        compClass = typeof(CompKwazaariFireMode);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef))
        {
            yield return error;
        }
        if (modes.Count == 0)
        {
            yield return "has no fire modes";
        }
        foreach (KwazaariFireMode mode in modes)
        {
            if (mode.projectile == null)
            {
                yield return $"fire mode {mode.labelKey} has no projectile";
            }
        }
    }
}

public sealed class CompKwazaariFireMode : ThingComp
{
    private int modeIndex;

    public CompProperties_KwazaariFireMode Props => (CompProperties_KwazaariFireMode)props;

    public KwazaariFireMode? Current => Props.modes.Count == 0 ? null : Props.modes[Mathf.Clamp(modeIndex, 0, Props.modes.Count - 1)];

    // Switching takes half the gun's reload time, but never less than this. The shotgun reloads one shell at a time,
    // so half its reload would be almost instant.
    private const float MinSwitchSeconds = 1f;

    private float? switchSeconds;

    public void SetMode(int index) => modeIndex = Mathf.Clamp(index, 0, Props.modes.Count - 1);

    // Read from CE's ammo user props by name, like the rest of the CE hooks.
    public float SwitchSeconds => switchSeconds ??= Mathf.Max(MinSwitchSeconds, ReadReloadSeconds() / 2f);

    private float ReadReloadSeconds()
    {
        CompProperties? ammoProps = parent.def.comps?.FirstOrDefault(c => c.GetType().FullName == "CombatExtended.CompProperties_AmmoUser");
        object? value = ammoProps == null ? null : AccessTools.Field(ammoProps.GetType(), "reloadTime")?.GetValue(ammoProps);
        return value is float seconds ? seconds : MinSwitchSeconds * 2f;
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref modeIndex, "fireMode");
    }

    // Only CompEquippable is asked for equipped gizmos, so the equipment tracker patch below asks this comp too.
    public IEnumerable<Gizmo> EquippedGizmos(Pawn pawn)
    {
        KwazaariFireMode? current = Current;
        if (current == null || pawn.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        // While a switch runs, a click moves on from the mode being switched to, not the old one.
        int from = pawn.CurJobDef == KwazaariFireModeCompat.SwitchJob && pawn.CurJob?.targetB.Thing == parent ? pawn.CurJob.count : modeIndex;
        List<FloatMenuOption> options = new();
        for (int i = 0; i < Props.modes.Count; i++)
        {
            int index = i;
            KwazaariFireMode mode = Props.modes[i];
            options.Add(new FloatMenuOption(mode.labelKey.Translate(), () => OrderSwitch(pawn, index), mode.Icon, Color.white));
        }
        yield return new Command_FireMode(options)
        {
            defaultLabel = "BOR_KwazaariFireMode".Translate(current.labelKey.Translate()),
            defaultDesc = "BOR_KwazaariFireModeDesc".Translate(SwitchSeconds.ToString("0.#")) + "\n\n" + current.descKey.Translate(),
            icon = current.Icon,
            action = () => OrderSwitch(pawn, (from + 1) % Props.modes.Count),
        };
    }

    private void OrderSwitch(Pawn pawn, int index)
    {
        if (index == modeIndex && pawn.CurJobDef != KwazaariFireModeCompat.SwitchJob)
        {
            return;
        }
        JobDef? jobDef = KwazaariFireModeCompat.SwitchJob;
        if (jobDef == null)
        {
            SetMode(index);
            return;
        }
        Job job = JobMaker.MakeJob(jobDef, pawn, parent);
        job.count = index;
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    private sealed class Command_FireMode(List<FloatMenuOption> options) : Command_Action
    {
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => options;
    }
}

// The pawn stops and reprograms the cartridges in the gun: target A is the pawn, B the gun, count the new mode.
public class JobDriver_SwitchKwazaariFireMode : JobDriver
{
    private Thing? Gun => job.GetTarget(TargetIndex.B).Thing;

    private CompKwazaariFireMode? Comp => Gun?.TryGetComp<CompKwazaariFireMode>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => Comp == null || pawn.equipment?.Primary != Gun);
        Toil work = Toils_General.Wait(Mathf.Max(1, (Comp?.SwitchSeconds ?? 1f).SecondsToTicks()), TargetIndex.None);
        work.WithProgressBarToilDelay(TargetIndex.A);
        yield return work;
        yield return Toils_General.Do(() => Comp?.SetMode(job.count));
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.GetGizmos))]
public static class Pawn_EquipmentTracker_GetGizmos_KwazaariFireMode
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn_EquipmentTracker __instance)
    {
        foreach (Gizmo gizmo in gizmos)
        {
            yield return gizmo;
        }
        CompKwazaariFireMode? comp = __instance.Primary?.GetComp<CompKwazaariFireMode>();
        if (comp == null)
        {
            yield break;
        }
        foreach (Gizmo gizmo in comp.EquippedGizmos(__instance.pawn))
        {
            yield return gizmo;
        }
    }
}

// CE picks a gun's projectile from its ammo set through CompAmmoUser.CurAmmoProjectile. CE is never referenced, only
// patched by name, so BOR still loads without it. If the name changes in an update, the weapons fire normal rounds.
[StaticConstructorOnStartup]
public static class KwazaariFireModeCompat
{
    private const string PackageId = "ceteam.combatextended";

    // Defined in the CE folder, so it only exists with CE. Without it the mode switches at once.
    public static readonly JobDef? SwitchJob = DefDatabase<JobDef>.GetNamedSilentFail("BOR_SwitchKwazaariFireMode");

    private static FieldInfo? gizmoAmmoField;
    private static PropertyInfo? curMagCountProp;
    private static PropertyInfo? magSizeProp;
    private static PropertyInfo? hasMagazineProp;

    static KwazaariFireModeCompat()
    {
        if (!ModsConfig.IsActive(PackageId))
        {
            return;
        }
        try
        {
            PatchAmmoCounter();
        }
        catch (Exception e)
        {
            Log.Error($"[BOR] Kwazaari gauss ammo counter failed; it counts cartridges instead of shots. {e}");
        }
        try
        {
            Type? ammoUser = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
            MethodInfo? getter = ammoUser == null ? null : AccessTools.PropertyGetter(ammoUser, "CurAmmoProjectile");
            MethodInfo? shotFired = ammoUser == null ? null : AccessTools.Method(ammoUser, "Notify_ShotFired", new[] { typeof(int) });
            if (getter == null || shotFired == null)
            {
                Log.Warning("[BOR] Combat Extended has changed; kwazaari gauss fire modes are off and the weapons fire normal rounds.");
                return;
            }
            Harmony harmony = new("bambaryla.bor.redux.kwazaarifiremode");
            harmony.Patch(getter, postfix: new HarmonyMethod(AccessTools.Method(typeof(KwazaariFireModeCompat), nameof(CurAmmoProjectilePostfix))));
            harmony.Patch(shotFired, prefix: new HarmonyMethod(AccessTools.Method(typeof(KwazaariFireModeCompat), nameof(ShotFiredPrefix))));
        }
        catch (Exception e)
        {
            Log.Error($"[BOR] Kwazaari gauss fire modes failed; the weapons fire normal rounds. {e}");
        }
    }

    private static void CurAmmoProjectilePostfix(ThingComp __instance, ref ThingDef __result)
    {
        ThingDef? projectile = __instance.parent?.GetComp<CompKwazaariFireMode>()?.Current?.projectile;
        if (projectile != null)
        {
            __result = projectile;
        }
    }

    // CE's magazine readout shows "cartridges / magazine size". In a mode that uses several cartridges per shot it shows
    // shots instead, rounded up because the last shot fires on whatever is left.
    private static void PatchAmmoCounter()
    {
        Type? gizmo = AccessTools.TypeByName("CombatExtended.GizmoAmmoStatus");
        Type? ammoUser = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
        gizmoAmmoField = gizmo == null ? null : AccessTools.Field(gizmo, "compAmmo");
        curMagCountProp = ammoUser == null ? null : AccessTools.Property(ammoUser, "CurMagCount");
        magSizeProp = ammoUser == null ? null : AccessTools.Property(ammoUser, "MagSize");
        hasMagazineProp = ammoUser == null ? null : AccessTools.Property(ammoUser, "HasMagazine");
        MethodInfo? barLabel = gizmo == null ? null : AccessTools.PropertyGetter(gizmo, "BarLabel");
        if (gizmoAmmoField == null || curMagCountProp == null || magSizeProp == null || hasMagazineProp == null || barLabel == null)
        {
            Log.Warning("[BOR] Combat Extended has changed; the kwazaari gauss ammo counter counts cartridges instead of shots.");
            return;
        }
        new Harmony("bambaryla.bor.redux.kwazaariammocounter").Patch(barLabel,
            postfix: new HarmonyMethod(AccessTools.Method(typeof(KwazaariFireModeCompat), nameof(BarLabelPostfix))));
    }

    private static void BarLabelPostfix(object __instance, ref string __result)
    {
        if (gizmoAmmoField!.GetValue(__instance) is not ThingComp ammo || hasMagazineProp!.GetValue(ammo) is not true)
        {
            return;
        }
        int cost = ammo.parent?.GetComp<CompKwazaariFireMode>()?.Current?.ammoPerShot ?? 1;
        if (cost <= 1)
        {
            return;
        }
        if (curMagCountProp!.GetValue(ammo) is not int current || magSizeProp!.GetValue(ammo) is not int size)
        {
            return;
        }
        __result = $"{(current + cost - 1) / cost} / {(size + cost - 1) / cost}";
    }

    // CE takes the count off the magazine and stops at 0, so the last shot of a magazine may cost less than the full price.
    private static void ShotFiredPrefix(ThingComp __instance, ref int ammoConsumedPerShot)
    {
        int cost = __instance.parent?.GetComp<CompKwazaariFireMode>()?.Current?.ammoPerShot ?? 1;
        if (cost > 1)
        {
            ammoConsumedPerShot = Math.Max(ammoConsumedPerShot, 1) * cost;
        }
    }
}
