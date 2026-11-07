using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public enum ExoskeletonMode
{
    Camo,
    Utility,
    Combat,
    None
}

// Keeps one locked apparel per exoskeleton mode on the pawn: index = (int)ExoskeletonMode.
public class HediffCompProperties_UmbralModeApparel : HediffCompProperties
{
    public List<ThingDef> apparelByMode = new();

    // How long each mode lasts before the exoskeleton reverts to no mode: index = (int)ExoskeletonMode. No entry, no limit.
    public List<float> hoursByMode = new();

    // Only the exoskeleton owns the mode and its gizmo; the exoskull follows it.
    public bool ownsMode;

    // After a timed mode runs out, no mode can be chosen for this long.
    public float expiryLockSeconds = 30f;

    public HediffCompProperties_UmbralModeApparel()
    {
        compClass = typeof(HediffComp_UmbralModeApparel);
    }
}

public class HediffComp_UmbralModeApparel : HediffComp
{
    private const int CheckInterval = 250;

    private const int AiCheckInterval = 60;

    // Severity of the exoskeleton hediff per mode; its stages carry the mode's stats.
    private static readonly float[] ModeSeverity = { 1f, 2f, 3f, 0.5f };

    private ExoskeletonMode mode = ExoskeletonMode.None;
    private int modeEndTick;

    // No mode can be chosen until this tick; set when a weapon breaks the armor.
    private int modeLockEndTick;

    public HediffCompProperties_UmbralModeApparel Props => (HediffCompProperties_UmbralModeApparel)props;

    public ExoskeletonMode Mode => Props.ownsMode ? mode : OwnerMode(Pawn);

    private static ExoskeletonMode OwnerMode(Pawn pawn)
    {
        Hediff? exo = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskeleton);
        return exo?.TryGetComp<HediffComp_UmbralModeApparel>()?.mode ?? ExoskeletonMode.None;
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
            if (Props.ownsMode && mode != ExoskeletonMode.None && Find.TickManager.TicksGame >= modeEndTick)
            {
                EndMode();
            }
            EnsureApparel();
        }
        if (Props.ownsMode && Pawn.IsHashIntervalTick(AiCheckInterval, delta))
        {
            AiEnterCombat();
        }
    }

    // Umbrals the player does not control put their combat armor on when a threat shows up.
    private void AiEnterCombat()
    {
        Pawn pawn = Pawn;
        if (mode != ExoskeletonMode.None || ModeLocked || pawn.Faction == Faction.OfPlayer || !pawn.Spawned || pawn.Downed)
        {
            return;
        }
        bool threatened = pawn.mindState?.enemyTarget != null
            || (pawn.Faction != null && GenHostility.AnyHostileActiveThreatTo(pawn.Map, pawn.Faction));
        if (threatened)
        {
            SetMode(ExoskeletonMode.Combat);
        }
    }

    public void SetMode(ExoskeletonMode newMode) => ApplyMode(newMode);

    private void EndMode()
    {
        ExoskeletonMode ended = mode;
        ApplyMode(ExoskeletonMode.None);
        modeLockEndTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Props.expiryLockSeconds * GenTicks.TicksPerRealSecond);
        if (Pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("BOR_ExoModeEnded".Translate(Pawn.LabelShortCap, ("BOR_ExoMode_" + ended).Translate()), Pawn, MessageTypeDefOf.NeutralEvent);
        }
    }

    public bool ModeLocked => Find.TickManager.TicksGame < modeLockEndTick;

    // The swarm AI hides in camo and ends it itself. Ending it locks the armor like a natural expiry, so the Umbral waits for it to come back.
    public void StartAiCamo() => ApplyMode(ExoskeletonMode.Camo);

    public void EndAiCamo()
    {
        if (mode == ExoskeletonMode.Camo)
        {
            EndMode();
        }
    }

    // Forces the combat armor off and keeps it off for lockTicks. Does nothing unless this is the exoskeleton in combat mode.
    public bool TryDisableCombat(int lockTicks)
    {
        if (!Props.ownsMode || mode != ExoskeletonMode.Combat)
        {
            return false;
        }
        modeLockEndTick = Find.TickManager.TicksGame + lockTicks;
        ApplyMode(ExoskeletonMode.None);
        if (Pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("BOR_ExoArmorBroken".Translate(Pawn.LabelShortCap), Pawn, MessageTypeDefOf.NegativeEvent);
        }
        return true;
    }

    private int ModeTicks(ExoskeletonMode m) =>
        (int)m < Props.hoursByMode.Count ? Mathf.RoundToInt(Props.hoursByMode[(int)m] * GenDate.TicksPerHour) : 0;

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
            // Saves from before the UnlockAll patch can hold unlocked pieces.
            pawn.apparel.Lock(worn[0]);
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
        modeEndTick = Find.TickManager.TicksGame + ModeTicks(newMode);
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
            defaultLabel = "BOR_ExoMode".Translate(("BOR_ExoMode_" + mode).Translate() + TimeLeftSuffix),
            defaultDesc = "BOR_ExoModeDesc".Translate(),
            icon = icon.uiIcon,
            action = () =>
            {
                List<FloatMenuOption> options = new();
                foreach (ExoskeletonMode m in System.Enum.GetValues(typeof(ExoskeletonMode)))
                {
                    ExoskeletonMode chosen = m;
                    int ticks = ModeTicks(chosen);
                    string label = ("BOR_ExoMode_" + chosen).Translate();
                    if (ModeLocked)
                    {
                        options.Add(new FloatMenuOption(label + " (" + "BOR_ExoModeLocked".Translate((modeLockEndTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()) + ")", null));
                        continue;
                    }
                    options.Add(new FloatMenuOption(ticks > 0 ? label + " (" + ticks.ToStringTicksToPeriod() + ")" : label, () => ApplyMode(chosen)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
    }

    private string TimeLeftSuffix =>
        Props.ownsMode && mode != ExoskeletonMode.None ? ", " + (modeEndTick - Find.TickManager.TicksGame).ToStringTicksToPeriod() : "";

    public override string CompLabelInBracketsExtra => ("BOR_ExoMode_" + Mode).Translate() + TimeLeftSuffix;

    public override void CompExposeData()
    {
        base.CompExposeData();
        // Saves from before no mode left utility unwritten, so it stays the default here. It expires on the first check.
        Scribe_Values.Look(ref mode, "exoMode", ExoskeletonMode.Utility);
        Scribe_Values.Look(ref modeEndTick, "exoModeEndTick");
        Scribe_Values.Look(ref modeLockEndTick, "exoCombatLockEndTick");
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

    // Pod forms by index in HediffCompProperties_UmbralUtilityPod.weapons.
    private const int Blade = 0;
    private const int Gun = 2;
    private const int Launcher = 3;

    private const int AiCheckInterval = 60;
    private const int AiSwitchCooldown = 180;
    private const float MeleeRange = 3f;
    private const float LauncherMinRange = 10f;
    private const float ClusterRadius = 3.9f;
    private const int ClusterSize = 3;

    private int lastAiSwitchTick;

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
        if (Pawn.IsHashIntervalTick(AiCheckInterval, delta))
        {
            AiChooseForm();
        }
    }

    // Umbrals the player does not control keep their distance: gun or launcher from afar, blade up close.
    private void AiChooseForm()
    {
        Pawn pawn = Pawn;
        if (pawn.Faction == Faction.OfPlayer || !pawn.Spawned || pawn.Downed || Props.weapons.Count <= Launcher)
        {
            return;
        }
        Thing? target = pawn.mindState?.enemyTarget;
        // Hiding in camo: no switching, a switch would end the walk to cover.
        if (target == null || !target.Spawned || Find.TickManager.TicksGame < lastAiSwitchTick + AiSwitchCooldown
            || HediffComp_UmbralSwarmAI.StateOf(pawn) != SwarmState.None)
        {
            return;
        }
        float distance = pawn.Position.DistanceTo(target.Position);
        int want = distance <= MeleeRange ? Blade : distance >= LauncherMinRange && IsClustered(pawn, target) ? Launcher : Gun;
        // The dark bolt does the launcher's job for free, and UmbralAbilityAI casts it.
        if (want == Launcher && UmbralAbilityAI.CanCast(pawn, BOR_DarkAbilityDefOf.BOR_DarkBolt))
        {
            want = Gun;
        }
        if (want == mode)
        {
            return;
        }
        mode = want;
        lastAiSwitchTick = Find.TickManager.TicksGame;
        EnsureWeapon();
        pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
    }

    // The swarm AI picks the weapon itself while hiding: the gun, or the launcher.
    public void AiEquip(bool launcher)
    {
        int want = launcher ? Launcher : Gun;
        if (Props.weapons.Count <= Launcher || mode == want)
        {
            return;
        }
        mode = want;
        lastAiSwitchTick = Find.TickManager.TicksGame;
        EnsureWeapon();
    }

    public float LauncherRange => Props.weapons.Count <= Launcher ? 0f : Props.weapons[Launcher].Verbs.FirstOrDefault()?.range ?? 0f;

    private static bool IsClustered(Pawn pawn, Thing target)
    {
        int count = 0;
        foreach (Thing t in GenRadial.RadialDistinctThingsAround(target.Position, target.Map, ClusterRadius, true))
        {
            if (t is Pawn other && !other.Downed && !other.Dead && other.HostileTo(pawn) && ++count >= ClusterSize)
            {
                return true;
            }
        }
        return false;
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
