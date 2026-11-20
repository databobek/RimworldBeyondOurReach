using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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

    // Only the exoskeleton owns the mode and its gizmo; the exoskull follows it.
    public bool ownsMode;

    // One pool of mode time shared by every mode. It drains in any mode and refills at rechargeRate (hours per hour) in no mode.
    public float maxHours = 4f;
    public float rechargeRate = 1f;

    // When the pool runs dry the exoskeleton drops to no mode and cannot be used, nor recharge, for this long.
    public float recoveryHours = 2f;

    // A player switch drops to no form at once. The old form's armor then sheds from the bottom up over dismantleSeconds,
    // and the new form's armor grows on from the top down over formSeconds (game time at 1x), after which the form applies.
    public float dismantleSeconds = 1f;
    public float formSeconds = 1.5f;

    // Entering a form needs at least this much charge, so a form never starts only to run dry at once. Same for the AI.
    public float minChargeHours = 0.5f;

    // Umbrals the player does not control: they go into combat form when an enemy is within aiThreatRange (or they have
    // a target), and back to no form after aiCalmSeconds with none near, so the charge refills between fights.
    public float aiThreatRange = 30f;
    public float aiCalmSeconds = 15f;

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

    // Mode time left, in ticks; negative until first read, which means full.
    private float charge = -1f;

    // No mode can be chosen, and the pool does not refill, until this tick: set when the pool runs dry or a weapon breaks the armor.
    private int modeLockEndTick;

    // A switch in progress: the form being shed and the form being grown on, as (int)ExoskeletonMode, or -1 for no form.
    // The pawn is in no form for the whole switch. Shedding runs to dismantleEndTick, growing from there to switchEndTick.
    private bool switching;
    private int switchFrom = -1;
    private int switchTo = -1;
    private int switchStartTick;
    private int dismantleEndTick;
    private int switchEndTick;

    // AI: the last tick an enemy was near.
    private int aiLastThreatTick = -999999;

    public HediffCompProperties_UmbralModeApparel Props => (HediffCompProperties_UmbralModeApparel)props;

    private float MaxCharge => Props.maxHours * GenDate.TicksPerHour;

    private float Charge => charge < 0f ? MaxCharge : charge;

    public float ChargeTicks => Charge;

    private float MinCharge => Props.minChargeHours * GenDate.TicksPerHour;

    // Whether a form can be picked now: not recovering, not mid-switch, and enough charge to be worth it.
    public bool CanStartForm => !ModeLocked && !switching && Charge >= MinCharge;

    public ExoskeletonMode Mode => Props.ownsMode ? mode : OwnerMode(Pawn);

    public bool Switching => switching;

    // For the effect: which form's armor to draw right now and how much of it shows, as the visible fraction from the top
    // of the texture. Shedding goes 1 -> 0 (the bottom edge rises), growing goes 0 -> 1 (the bottom edge comes down).
    public bool TryGetSwitchFrame(out int form, out float shown)
    {
        form = -1;
        shown = 0f;
        if (!switching)
        {
            return false;
        }
        int now = Find.TickManager.TicksGame;
        if (switchFrom >= 0 && now < dismantleEndTick)
        {
            form = switchFrom;
            shown = 1f - Fraction(now, switchStartTick, dismantleEndTick);
            return true;
        }
        if (switchTo >= 0)
        {
            form = switchTo;
            shown = Fraction(now, dismantleEndTick, switchEndTick);
            return true;
        }
        return false;
    }

    private static float Fraction(int now, int start, int end) => Mathf.Clamp01((now - start) / (float)Mathf.Max(1, end - start));

    // The piece this comp puts on for a form; the exoskull's comp answers for the helmet.
    public ThingDef? ApparelFor(int form) => form >= 0 && form < Props.apparelByMode.Count ? Props.apparelByMode[form] : null;

    public HediffComp_UmbralModeApparel? SkullComp =>
        Pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskull)?.TryGetComp<HediffComp_UmbralModeApparel>();

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
        if (switching)
        {
            if (Find.TickManager.TicksGame >= switchEndTick)
            {
                // ApplyMode ends the switch.
                ApplyMode(switchTo >= 0 ? (ExoskeletonMode)switchTo : ExoskeletonMode.None);
            }
            else if (Pawn.Spawned)
            {
                // Also picks the effect back up after a load.
                MapComponent_ExoskeletonForms.Of(Pawn.Map).Add(this);
            }
        }
        if (Pawn.IsHashIntervalTick(CheckInterval, delta))
        {
            if (Props.ownsMode)
            {
                TickCharge();
            }
            EnsureApparel();
        }
        if (Props.ownsMode && Pawn.IsHashIntervalTick(AiCheckInterval, delta))
        {
            AiTick();
        }
    }

    // Umbrals the player does not control play by the player's rules: the same switch, charge and recovery. They put the
    // combat armor on when an enemy comes near and take it off once things are calm, to recharge.
    private void AiTick()
    {
        Pawn pawn = Pawn;
        if (pawn.Faction == Faction.OfPlayer || !pawn.Spawned || pawn.Downed || switching
            || HediffComp_UmbralSwarmAI.StateOf(pawn) != SwarmState.None)
        {
            // While the swarm AI hides, it drives the suit itself.
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (EnemyNear(pawn))
        {
            aiLastThreatTick = now;
        }
        bool calm = now - aiLastThreatTick >= SecondsToTicks(Props.aiCalmSeconds);
        if (mode == ExoskeletonMode.None)
        {
            if (!calm && CanStartForm)
            {
                RequestMode(ExoskeletonMode.Combat);
            }
        }
        else if (calm)
        {
            RequestMode(ExoskeletonMode.None);
        }
    }

    private bool EnemyNear(Pawn pawn)
    {
        if (pawn.mindState?.enemyTarget != null)
        {
            return true;
        }
        float range = Props.aiThreatRange;
        foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (!other.Dead && !other.Downed && other.Position.InHorDistOf(pawn.Position, range) && other.HostileTo(pawn))
            {
                return true;
            }
        }
        return false;
    }

    // Runs every CheckInterval ticks: any mode drains the pool, no mode refills it unless the exoskeleton is recovering.
    private void TickCharge()
    {
        if (mode != ExoskeletonMode.None)
        {
            charge = Charge - CheckInterval;
            if (charge <= 0f)
            {
                charge = 0f;
                EndMode();
            }
        }
        else if (!ModeLocked)
        {
            charge = Mathf.Min(MaxCharge, Charge + CheckInterval * Props.rechargeRate);
        }
    }

    // The pool ran dry: back to no mode, and the counting only resumes after the recovery phase.
    private void EndMode()
    {
        ApplyMode(ExoskeletonMode.None);
        modeLockEndTick = Find.TickManager.TicksGame + Mathf.RoundToInt(Props.recoveryHours * GenDate.TicksPerHour);
        if (Pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("BOR_ExoChargeEmpty".Translate(Pawn.LabelShortCap), Pawn, MessageTypeDefOf.NegativeEvent);
        }
        else
        {
            FleeSpent();
        }
    }

    // An Umbral of another faction whose suit ran dry on the player's map is armorless for hours, so it gives up and
    // leaves. Heralds have their own script and stay.
    private void FleeSpent()
    {
        Pawn pawn = Pawn;
        if (pawn.Faction == null || !pawn.Spawned || pawn.Downed || !pawn.Map.IsPlayerHome || UmbralHerald.IsHerald(pawn)
            || IsFleeing(pawn))
        {
            return;
        }
        pawn.GetLord()?.RemovePawn(pawn);
        LordMaker.MakeNewLord(pawn.Faction, new LordJob_ExitMapBest(LocomotionUrgency.Sprint, canDig: false, canDefendSelf: true), pawn.Map, new[] { pawn });
        pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
        if (pawn.HostileTo(Faction.OfPlayer))
        {
            Messages.Message("BOR_ExoSpentFlee".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
        }
    }

    // True while this Umbral is leaving because its suit ran dry; the swarm AI stands down for it.
    public static bool IsFleeing(Pawn pawn) => pawn.GetLord()?.LordJob is LordJob_ExitMapBest;

    public bool ModeLocked => Find.TickManager.TicksGame < modeLockEndTick;

    // The swarm AI hides in camo and ends it itself, through the same switch the player uses.
    public void StartAiCamo() => RequestMode(ExoskeletonMode.Camo);

    public void EndAiCamo()
    {
        if (mode == ExoskeletonMode.Camo)
        {
            RequestMode(ExoskeletonMode.None);
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

    // A corpse must not keep the camo: death nulls the pawn's mindState, which the vanilla invisibility comp reads on
    // every draw of the corpse and the portrait.
    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null!)
    {
        base.Notify_PawnDied(dinfo, culprit);
        if (Props.ownsMode && (mode != ExoskeletonMode.None || switching))
        {
            ApplyMode(ExoskeletonMode.None);
        }
    }

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

    // The player's switch: drop to no form at once, shed the old form's armor, then grow the new one on. Switching to no
    // form only sheds.
    private void RequestMode(ExoskeletonMode target)
    {
        ExoskeletonMode from = mode;
        ApplyMode(ExoskeletonMode.None);
        switchFrom = from != ExoskeletonMode.None ? (int)from : -1;
        switchTo = target != ExoskeletonMode.None ? (int)target : -1;
        if (switchFrom < 0 && switchTo < 0)
        {
            return;
        }
        switching = true;
        switchStartTick = Find.TickManager.TicksGame;
        dismantleEndTick = switchStartTick + (switchFrom >= 0 ? SecondsToTicks(Props.dismantleSeconds) : 0);
        switchEndTick = dismantleEndTick + (switchTo >= 0 ? SecondsToTicks(Props.formSeconds) : 0);
        if (Pawn.Spawned)
        {
            MapComponent_ExoskeletonForms.Of(Pawn.Map).Add(this);
        }
    }

    internal static int SecondsToTicks(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * GenTicks.TicksPerRealSecond));

    // Any direct change, including the end of a switch, settles the form and ends a switch in progress.
    private void ApplyMode(ExoskeletonMode newMode)
    {
        switching = false;
        switchFrom = -1;
        switchTo = -1;
        mode = newMode;
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
        // The label would cover the icon, so it goes into the tooltip.
        Command_Action cmd = new()
        {
            defaultLabel = "",
            defaultDesc = "BOR_ExoMode".Translate(ModeLabel + TimeLeftSuffix) + "\n\n" + "BOR_ExoModeDesc".Translate(),
            icon = mode switch
            {
                ExoskeletonMode.Camo => BorIcons.BodysuitCamo,
                ExoskeletonMode.Utility => BorIcons.BodysuitWork,
                ExoskeletonMode.Combat => BorIcons.BodysuitArmor,
                _ => icon.uiIcon,
            },
            action = () =>
            {
                List<FloatMenuOption> options = new();
                foreach (ExoskeletonMode m in System.Enum.GetValues(typeof(ExoskeletonMode)))
                {
                    ExoskeletonMode chosen = m;
                    if (chosen == mode)
                    {
                        continue;
                    }
                    string label = ("BOR_ExoMode_" + chosen).Translate();
                    if (chosen != ExoskeletonMode.None && !CanStartForm)
                    {
                        options.Add(new FloatMenuOption(label + " (" + "BOR_ExoNeedsCharge".Translate(((int)MinCharge).ToStringTicksToPeriod()) + ")", null));
                        continue;
                    }
                    options.Add(new FloatMenuOption(label, () => RequestMode(chosen)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        if (ModeLocked)
        {
            cmd.Disable("BOR_ExoModeLocked".Translate((modeLockEndTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()));
        }
        else if (switching)
        {
            cmd.Disable(ModeLabel);
        }
        yield return cmd;
    }

    private string ModeLabel => switching && switchTo >= 0
        ? "BOR_ExoForming".Translate(("BOR_ExoMode_" + (ExoskeletonMode)switchTo).Translate()).Resolve()
        : ("BOR_ExoMode_" + Mode).Translate().Resolve();

    private string TimeLeftSuffix =>
        Props.ownsMode ? ", " + "BOR_ExoCharge".Translate(((int)Charge).ToStringTicksToPeriod(), ((int)MaxCharge).ToStringTicksToPeriod()) : "";

    public override string CompLabelInBracketsExtra => (Props.ownsMode ? ModeLabel : ("BOR_ExoMode_" + Mode).Translate().Resolve()) + TimeLeftSuffix;

    public override void CompExposeData()
    {
        base.CompExposeData();
        // Saves from before no mode left utility unwritten, so it stays the default here. It expires on the first check.
        Scribe_Values.Look(ref mode, "exoMode", ExoskeletonMode.Utility);
        Scribe_Values.Look(ref charge, "exoCharge", -1f);
        Scribe_Values.Look(ref modeLockEndTick, "exoCombatLockEndTick");
        Scribe_Values.Look(ref switching, "exoSwitching");
        Scribe_Values.Look(ref switchFrom, "exoSwitchFrom", -1);
        Scribe_Values.Look(ref switchTo, "exoSwitchTo", -1);
        Scribe_Values.Look(ref switchStartTick, "exoSwitchStart");
        Scribe_Values.Look(ref dismantleEndTick, "exoSwitchDismantleEnd");
        Scribe_Values.Look(ref switchEndTick, "exoSwitchEnd");
        Scribe_Values.Look(ref aiLastThreatTick, "exoAiLastThreat", -999999);
    }
}

// The utility pod renders as a weapon: one ammo-free weapon per mode, re-equipped whenever it goes missing.
public class HediffCompProperties_UmbralUtilityPod : HediffCompProperties
{
    public List<ThingDef> weapons = new();

    // Forms kept in `weapons` (the AI goes by index) but left out of the switch menu.
    public List<ThingDef> disabledWeapons = new();

    // A form change empties the hand while the pod morphs from the old shape into the new one, over this many seconds
    // (game time at 1x). Matches the exoskeleton's shed and grow.
    public float morphSeconds = 2.5f;

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

    // A form change in progress: the pod morphs from the form morphFrom into the form `mode`, and the hand holds nothing
    // until morphEndTick.
    private bool morphing;
    private int morphFrom = -1;
    private int morphStartTick;
    private int morphEndTick;

    public HediffCompProperties_UmbralUtilityPod Props => (HediffCompProperties_UmbralUtilityPod)props;

    private ThingDef? Wanted => FormDef(mode);

    private ThingDef? FormDef(int form) => Props.weapons.Count == 0 ? null : Props.weapons[Mathf.Clamp(form, 0, Props.weapons.Count - 1)];

    public bool Morphing => morphing;

    // Never-equipped copies of the forms, only used to ask the weapon renderer (and the mods that move weapons) where each
    // form would be drawn. They claim the pawn's equipment as their holder so those mods treat them as the pawn's weapon.
    private readonly Dictionary<ThingDef, ThingWithComps> poseProxies = new();

    public ThingWithComps? PoseProxy(ThingDef def)
    {
        if (Pawn.equipment == null)
        {
            return null;
        }
        if (!poseProxies.TryGetValue(def, out ThingWithComps? proxy))
        {
            proxy = (ThingWithComps)ThingMaker.MakeThing(def);
            poseProxies[def] = proxy;
        }
        proxy.holdingOwner = Pawn.equipment.GetDirectlyHeldThings();
        return proxy;
    }

    // For the effect: the shape being left, the shape being taken, and how far the morph has got, 0 to 1.
    public bool TryGetMorphFrame(out ThingDef? from, out ThingDef? to, out float progress)
    {
        from = FormDef(morphFrom);
        to = Wanted;
        progress = 0f;
        if (!morphing || from == null || to == null)
        {
            return false;
        }
        progress = Mathf.Clamp01((Find.TickManager.TicksGame - morphStartTick) / (float)Mathf.Max(1, morphEndTick - morphStartTick));
        return true;
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        EnsureWeapon();
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (morphing)
        {
            if (Find.TickManager.TicksGame >= morphEndTick)
            {
                morphing = false;
                EnsureWeapon();
            }
            else
            {
                // Also picks the effect back up after a load.
                UtilityPodMorphs.Add(this);
            }
        }
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
        if (target == null || !target.Spawned || morphing || Find.TickManager.TicksGame < lastAiSwitchTick + AiSwitchCooldown
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
        lastAiSwitchTick = Find.TickManager.TicksGame;
        RequestForm(want);
        pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
    }

    // The swarm AI picks the weapon itself while hiding: the gun, or the launcher. It morphs under the camo.
    public void AiEquip(bool launcher)
    {
        int want = launcher ? Launcher : Gun;
        if (Props.weapons.Count <= Launcher || mode == want)
        {
            return;
        }
        lastAiSwitchTick = Find.TickManager.TicksGame;
        RequestForm(want);
    }

    // Every form change, the player's and the AI's: the weapon goes at once and the pod morphs into the new shape, which
    // is equipped when the morph ends. A new target mid-morph keeps the running timer.
    private void RequestForm(int want)
    {
        if (want == mode || Props.weapons.Count == 0)
        {
            return;
        }
        if (!morphing)
        {
            morphFrom = mode;
            morphStartTick = Find.TickManager.TicksGame;
            morphEndTick = morphStartTick + HediffComp_UmbralModeApparel.SecondsToTicks(Props.morphSeconds);
        }
        mode = want;
        morphing = true;
        EnsureWeapon();
        UtilityPodMorphs.Add(this);
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

    // A death mid-morph ends it: the form already counts as the new one, which a resurrected pawn grows back.
    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null!)
    {
        base.Notify_PawnDied(dinfo, culprit);
        morphing = false;
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();
        morphing = false;
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
        if (want == null || pawn.equipment == null)
        {
            return;
        }
        ThingWithComps? primary = pawn.equipment.Primary;
        // Mid-morph the hand is empty.
        if (morphing)
        {
            if (primary != null && Props.weapons.Contains(primary.def))
            {
                pawn.equipment.DestroyEquipment(primary);
                StopUsing(pawn, primary);
            }
            return;
        }
        if (pawn.Dead || pawn.Downed)
        {
            return;
        }
        if (primary?.def == want)
        {
            return;
        }
        if (primary != null)
        {
            if (Props.weapons.Contains(primary.def))
            {
                pawn.equipment.DestroyEquipment(primary);
                StopUsing(pawn, primary);
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

    // Removing equipment does not stop an attack already under way: a gun warming up or mid-burst, or a melee job, would
    // keep using the destroyed weapon's verb. Vanilla cancels the stance the same way when a primary is destroyed.
    private static void StopUsing(Pawn pawn, ThingWithComps weapon)
    {
        if (!pawn.Spawned)
        {
            return;
        }
        pawn.stances?.CancelBusyStanceSoft();
        if (pawn.CurJob?.verbToUse?.EquipmentSource == weapon)
        {
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        if (Pawn.Faction != Faction.OfPlayer || Props.weapons.Count == 0)
        {
            yield break;
        }
        ThingDef current = Wanted!;
        Command_Action cmd = new()
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
                    if (chosen != mode && !Props.disabledWeapons.Contains(Props.weapons[i]))
                    {
                        options.Add(new FloatMenuOption(Props.weapons[i].LabelCap, () => RequestForm(chosen)));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
        if (morphing)
        {
            cmd.Disable("BOR_ExoForming".Translate(current.label));
        }
        yield return cmd;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref mode, "podMode");
        Scribe_Values.Look(ref morphing, "podMorphing");
        Scribe_Values.Look(ref morphFrom, "podMorphFrom", -1);
        Scribe_Values.Look(ref morphStartTick, "podMorphStart");
        Scribe_Values.Look(ref morphEndTick, "podMorphEnd");
    }
}
