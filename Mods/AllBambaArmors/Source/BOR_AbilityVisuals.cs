using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using AllBambaArmors;

namespace BeyondOurReach
{
    /// <summary>Effects for the Orientis abilities: vaporize look and golden mist.</summary>
    public static class BOR_AbilityVisuals
    {
        /// <summary>Vanilla's vaporize look (heat distortion, glow, rays, radial sparks) without its damage. Scale 1 fits a 2.9-cell blast.</summary>
        public static void VaporizeBurst(IntVec3 cell, Map map, float scale, bool sound)
        {
            var effecter = EffecterDefOf.Vaporize_Heatwave.Spawn(cell, map, scale);
            effecter.Trigger(new TargetInfo(cell, map), TargetInfo.Invalid);
            effecter.Cleanup();
            if (sound)
                ABT_DefOf.Explosion_Vaporize?.PlayOneShot(new TargetInfo(cell, map));
        }

        private static readonly Color MistColor = new Color(1f, 0.78f, 0.28f, 0.85f);

        /// <summary>
        /// Slow drifting puffs of golden mist, the Orientis look (ABT_GoldenMist: smoke texture, glow shader).
        /// Spread through the radius, or kept to its edge to outline a bubble. Two puffs per requested puff, so it reads as thick.
        /// </summary>
        public static void GoldenMist(IntVec3 center, Map map, float radius, int puffs, bool edgeOnly = false)
        {
            var fleck = BOR_ArcaneDefOf.ABT_GoldenMist ?? FleckDefOf.Smoke;
            var origin = center.ToVector3Shifted();
            for (var i = 0; i < puffs * 2; i++)
            {
                var distance = edgeOnly ? radius * Rand.Range(0.85f, 1f) : radius * Mathf.Sqrt(Rand.Value);
                var loc = origin + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0f, 360f)) * distance;
                if (!loc.ShouldSpawnMotesAt(map))
                    continue;

                var data = FleckMaker.GetDataStatic(loc, map, fleck, Rand.Range(1.8f, 3f));
                data.instanceColor = MistColor;
                data.rotationRate = Rand.Range(-20f, 20f);
                data.velocityAngle = Rand.Range(0f, 360f);
                data.velocitySpeed = Rand.Range(0.1f, 0.3f);
                map.flecks.CreateFleck(data);
            }
        }
    }
}
