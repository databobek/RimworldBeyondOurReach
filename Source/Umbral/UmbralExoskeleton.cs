using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

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

    public HediffCompProperties_UmbralModeApparel()
    {
        compClass = typeof(HediffComp_UmbralModeApparel);
    }
}

public class HediffComp_UmbralModeApparel : HediffComp
{
    private const int CheckInterval = 250;

    // Severity of the exoskeleton hediff per mode; its stages carry the mode's stats.
    private static readonly float[] ModeSeverity = { 1f, 2f, 3f, 0.5f };

    private ExoskeletonMode mode = ExoskeletonMode.None;
    private int modeEndTick;

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
    }

    private void EndMode()
    {
        ExoskeletonMode ended = mode;
        ApplyMode(ExoskeletonMode.None);
        if (Pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("BOR_ExoModeEnded".Translate(Pawn.LabelShortCap, ("BOR_ExoMode_" + ended).Translate()), Pawn, MessageTypeDefOf.NeutralEvent);
        }
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
