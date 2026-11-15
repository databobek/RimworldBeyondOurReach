using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public enum SwarmState
{
    None,

    // Camo, gun in hand, in cover, picking targets with abilities.
    Cover,

    // Camo, launcher in hand, in cover within launcher range, until the camo time is up.
    Launcher,

    // Camo is off: fire the launcher from cover for strikeSeconds, then the suit AI puts the combat armor back on.
    Strike
}

public class HediffCompProperties_UmbralSwarmAI : HediffCompProperties
{
    // Hiding starts when at least this many enemies are within enemyRange.
    public int minEnemies = 5;
    public float enemyRange = 35f;

    // Seconds in camo with the gun, in cover, before moving up to launcher range.
    public float coverSeconds = 8f;

    // Total seconds in camo, counted from the start of the switch into it. Hiding needs at least this much suit charge.
    public float camoSeconds = 20f;

    // Seconds firing from cover in no form after the camo is dropped.
    public float strikeSeconds = 30f;

    // After coming out, no new hiding for this long.
    public float cooldownSeconds = 60f;

    // The launcher cell must be within this share of the launcher range of the nearest enemy.
    public float launcherRangeShare = 0.9f;

    public float coverSearchRadius = 16f;

    public HediffCompProperties_UmbralSwarmAI()
    {
        compClass = typeof(HediffComp_UmbralSwarmAI);
    }
}

// Umbrals the player does not control, outnumbered: camo and cover are the rethink mode.
// Gun, camo and cover with abilities; then launcher range, waiting for the camo to drop; then fire from cover until the armor is back.
public class HediffComp_UmbralSwarmAI : HediffComp
{
    private const int CheckInterval = 60;
    private const int MaxThreatsForCover = 8;
    private const int MaxLineOfSightChecks = 50;
    private const int MaxReachChecks = 10;

    private SwarmState state;
    private int cooldownEndTick;
    private int phaseStartTick;
    private int camoStartTick;
    private IntVec3 coverCell = IntVec3.Invalid;

    public HediffCompProperties_UmbralSwarmAI Props => (HediffCompProperties_UmbralSwarmAI)props;

    public SwarmState State => state;

    public static SwarmState StateOf(Pawn pawn) =>
        pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_Umbral)?.TryGetComp<HediffComp_UmbralSwarmAI>()?.state ?? SwarmState.None;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (Pawn.IsHashIntervalTick(CheckInterval, delta))
        {
            AiTick();
        }
    }

    private void AiTick()
    {
        Pawn pawn = Pawn;
        if (pawn.Faction == Faction.OfPlayer || !pawn.Spawned || pawn.Downed || pawn.Dead || UmbralHerald.IsHerald(pawn)
            || HediffComp_UmbralModeApparel.IsFleeing(pawn))
        {
            state = SwarmState.None;
            return;
        }
        HediffComp_UmbralModeApparel? exo = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralExoskeleton)?.TryGetComp<HediffComp_UmbralModeApparel>();
        if (exo == null)
        {
            state = SwarmState.None;
            return;
        }
        int now = Find.TickManager.TicksGame;
        HediffComp_UmbralUtilityPod? pod = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_UmbralDefOf.BOR_UmbralUtilityPod)?.TryGetComp<HediffComp_UmbralUtilityPod>();
        switch (state)
        {
            case SwarmState.None:
                // Same rules as the player: a switch, and enough charge to last the whole hide.
                if (now >= cooldownEndTick && exo.CanStartForm && exo.ChargeTicks >= Ticks(Props.camoSeconds)
                    && Threats(pawn).Count >= Props.minEnemies)
                {
                    pod?.AiEquip(launcher: false);
                    exo.StartAiCamo();
                    camoStartTick = now;
                    SetState(SwarmState.Cover, now, resetCover: true);
                }
                return;
            case SwarmState.Cover:
                // Still switching into camo counts as camo.
                if (exo.Mode != ExoskeletonMode.Camo && !exo.Switching)
                {
                    SetState(SwarmState.Strike, now, resetCover: false);
                }
                else if (now >= phaseStartTick + Ticks(Props.coverSeconds))
                {
                    pod?.AiEquip(launcher: true);
                    SetState(SwarmState.Launcher, now, resetCover: true);
                }
                break;
            case SwarmState.Launcher:
                if ((exo.Mode != ExoskeletonMode.Camo && !exo.Switching) || now >= camoStartTick + Ticks(Props.camoSeconds))
                {
                    exo.EndAiCamo();
                    SetState(SwarmState.Strike, now, resetCover: false);
                }
                break;
            case SwarmState.Strike:
                if (now >= phaseStartTick + Ticks(Props.strikeSeconds))
                {
                    state = SwarmState.None;
                    cooldownEndTick = now + Ticks(Props.cooldownSeconds);
                    coverCell = IntVec3.Invalid;
                    pawn.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                    return;
                }
                break;
        }
        HoldCover(pawn, pod);
    }

    private static int Ticks(float seconds) => Mathf.RoundToInt(seconds * GenTicks.TicksPerRealSecond);

    private void SetState(SwarmState next, int now, bool resetCover)
    {
        state = next;
        phaseStartTick = now;
        if (resetCover)
        {
            coverCell = IntVec3.Invalid;
        }
    }

    private List<Pawn> Threats(Pawn pawn)
    {
        float rangeSquared = Props.enemyRange * Props.enemyRange;
        List<Pawn> result = new();
        foreach (Pawn other in pawn.Map.mapPawns.AllPawnsSpawned)
        {
            if (!other.Dead && !other.Downed && other.HostileTo(pawn) && other.Position.DistanceToSquared(pawn.Position) <= rangeSquared)
            {
                result.Add(other);
            }
        }
        return result;
    }

    // Runs to cover out of the enemies' sight, then waits there.
    private void HoldCover(Pawn pawn, HediffComp_UmbralUtilityPod? pod)
    {
        Job? current = pawn.CurJob;
        // Never cut a cast short.
        if (current?.ability != null)
        {
            return;
        }
        if (pawn.Position == coverCell)
        {
            if (current?.def != JobDefOf.Wait_Combat)
            {
                Job wait = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                wait.expiryInterval = 240;
                pawn.jobs.StartJob(wait, JobCondition.InterruptForced);
            }
            return;
        }
        if (coverCell.IsValid && current?.def == JobDefOf.Goto && current.targetA.Cell == coverCell)
        {
            return;
        }
        // With the launcher in hand, the cover must also be in its range.
        float launcherRange = state == SwarmState.Cover || pod == null ? 0f : pod.LauncherRange * Props.launcherRangeShare;
        coverCell = FindCover(pawn, Threats(pawn), launcherRange);
        if (!coverCell.IsValid)
        {
            return;
        }
        Job go = JobMaker.MakeJob(JobDefOf.Goto, coverCell);
        go.locomotionUrgency = LocomotionUrgency.Sprint;
        pawn.jobs.StartJob(go, JobCondition.InterruptForced);
    }

    // Prefers cells no threat can see, far from the threats, with solid cover around. Line of sight is the costly part, so
    // it is only traced for the cells that already score best on distance and cover.
    private IntVec3 FindCover(Pawn pawn, List<Pawn> threats, float launcherRange)
    {
        Map map = pawn.Map;
        List<Pawn> nearest = threats.OrderBy(t => t.Position.DistanceToSquared(pawn.Position)).Take(MaxThreatsForCover).ToList();
        List<(IntVec3 cell, float score)> scored = new();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, Props.coverSearchRadius, true))
        {
            if (!cell.InBounds(map) || !cell.Standable(map))
            {
                continue;
            }
            float nearestDistance = nearest.Count == 0 ? 0f : nearest.Min(t => t.Position.DistanceTo(cell));
            // Far from the enemies, or with the launcher, just inside its range of the nearest one.
            float distanceScore = launcherRange > 0f ? -Mathf.Max(0f, nearestDistance - launcherRange) * 10f : Mathf.Min(20f, nearestDistance);
            float cover = CoverUtility.TotalSurroundingCoverScore(cell, map);
            scored.Add((cell, distanceScore + cover * 5f - cell.DistanceTo(pawn.Position) * 0.5f));
        }
        IEnumerable<(IntVec3 cell, float score)> best = scored.OrderByDescending(s => s.score).Take(MaxLineOfSightChecks)
            .Select(s => (s.cell, s.score + Hidden(s.cell, nearest, map) * 100f));
        foreach ((IntVec3 cell, float score) in best.OrderByDescending(s => s.score).Take(MaxReachChecks))
        {
            if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
            {
                return cell;
            }
        }
        return IntVec3.Invalid;
    }

    // Share of the threats that cannot see the cell.
    private static float Hidden(IntVec3 cell, List<Pawn> threats, Map map) =>
        threats.Count == 0 ? 1f : threats.Count(t => !GenSight.LineOfSight(cell, t.Position, map)) / (float)threats.Count;

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref state, "swarmState");
        Scribe_Values.Look(ref cooldownEndTick, "swarmCooldownEndTick");
        Scribe_Values.Look(ref phaseStartTick, "swarmPhaseStartTick");
        Scribe_Values.Look(ref camoStartTick, "swarmCamoStartTick");
        Scribe_Values.Look(ref coverCell, "swarmCoverCell", IntVec3.Invalid);
    }
}
