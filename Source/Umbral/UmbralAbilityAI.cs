using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class HediffCompProperties_UmbralAbilityAI : HediffCompProperties
{
    public HediffCompProperties_UmbralAbilityAI()
    {
        compClass = typeof(HediffComp_UmbralAbilityAI);
    }
}

// Umbrals the player does not control use the dark abilities they have learned.
public class HediffComp_UmbralAbilityAI : HediffComp
{
    private const int CheckInterval = 60;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (Pawn.IsHashIntervalTick(CheckInterval, delta))
        {
            UmbralAbilityAI.Tick(Pawn);
        }
    }
}

public static class UmbralAbilityAI
{
    private const float BoltMinDistance = 8f;
    private const float BoltClusterRadius = 3.9f;
    private const int BoltClusterSize = 3;

    public static bool CanCast(Pawn pawn, AbilityDef def) => pawn.abilities?.GetAbility(def) is Ability ability && ability.CanCast;

    // One cast per check: thrall a random enemy, bolt a cluster, kill the farthest enemy, wound the closest.
    public static void Tick(Pawn pawn)
    {
        // Hiding in camo, the Umbral may have no target of its own yet, but it still picks off what it can see.
        bool hiding = HediffComp_UmbralSwarmAI.StateOf(pawn) != SwarmState.None;
        if (pawn.Faction == Faction.OfPlayer || !pawn.Spawned || pawn.Downed || pawn.Dead || (!hiding && pawn.mindState?.enemyTarget == null)
            || pawn.CurJob?.ability != null || pawn.abilities == null)
        {
            return;
        }
        List<Pawn> enemies = Enemies(pawn);
        if (enemies.Count == 0)
        {
            return;
        }
        if (TryCast(pawn, BOR_DarkAbilityDefOf.BOR_DarkThrall, enemies.InRandomOrder()) || TryBolt(pawn, enemies))
        {
            return;
        }
        if (TryCast(pawn, BOR_DarkAbilityDefOf.BOR_DarkKill, enemies.OrderByDescending(e => e.Position.DistanceToSquared(pawn.Position))))
        {
            return;
        }
        TryCast(pawn, BOR_DarkAbilityDefOf.BOR_DarkWound, enemies.OrderBy(e => e.Position.DistanceToSquared(pawn.Position)));
    }

    private static List<Pawn> Enemies(Pawn pawn)
    {
        float range = BOR_DarkAbilityDefOf.BOR_DarkWound.verbProperties.range;
        List<Pawn> result = new();
        foreach (Thing thing in GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, range, true))
        {
            if (thing is Pawn other && !other.Dead && !other.Downed && other.HostileTo(pawn) && GenSight.LineOfSight(pawn.Position, other.Position, pawn.Map))
            {
                result.Add(other);
            }
        }
        return result;
    }

    // Casts on the first target in the order the ability accepts.
    private static bool TryCast(Pawn pawn, AbilityDef def, IEnumerable<Pawn> targets)
    {
        if (pawn.abilities.GetAbility(def) is not Ability ability || !ability.CanCast)
        {
            return false;
        }
        foreach (Pawn target in targets)
        {
            if (ability.CanApplyOn(new LocalTargetInfo(target)))
            {
                Cast(pawn, ability, new LocalTargetInfo(target));
                return true;
            }
        }
        return false;
    }

    // Bolts the middle of a far cluster of enemies, but never with a friend inside the blast.
    private static bool TryBolt(Pawn pawn, List<Pawn> enemies)
    {
        if (pawn.abilities.GetAbility(BOR_DarkAbilityDefOf.BOR_DarkBolt) is not Ability ability || !ability.CanCast)
        {
            return false;
        }
        float blast = BOR_DarkAbilityDefOf.BOR_DarkBolt.EffectRadius;
        foreach (Pawn center in enemies.OrderByDescending(e => e.Position.DistanceToSquared(pawn.Position)))
        {
            if (pawn.Position.DistanceTo(center.Position) < BoltMinDistance
                || enemies.Count(e => e.Position.DistanceTo(center.Position) <= BoltClusterRadius) < BoltClusterSize
                || FriendInBlast(pawn, center.Position, blast)
                || !ability.CanApplyOn(new LocalTargetInfo(center.Position)))
            {
                continue;
            }
            Cast(pawn, ability, new LocalTargetInfo(center.Position));
            return true;
        }
        return false;
    }

    private static bool FriendInBlast(Pawn caster, IntVec3 cell, float radius) =>
        caster.Position.DistanceTo(cell) <= radius + 1f
        || GenRadial.RadialDistinctThingsAround(cell, caster.Map, radius, true).Any(t => t is Pawn p && !p.Dead && !p.HostileTo(caster));

    private static void Cast(Pawn pawn, Ability ability, LocalTargetInfo target)
    {
        Job job = ability.GetJob(target, target);
        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
    }
}
