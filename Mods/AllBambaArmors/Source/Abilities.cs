using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using VEF.Abilities;
using Verse;
using Ability = VEF.Abilities.Ability;
using Verse.AI;
using Verse.Sound;
using AllBambaArmors;
using AbilityUtility = AllBambaArmors.AbilityUtility;

namespace BeyondOurReach
{
    public class AbilityExtension_Burst : DefModExtension
    {
        public int count = 5;
        /// <summary>Energy for each projectile after the first. The burst stops early when the pool cannot pay.</summary>
        public float extraCost;
    }

    /// <summary>Fires several projectiles per cast; each takes the def's forced-miss spread, which grows with distance.</summary>
    public class Ability_Burst : Ability_ShootProjectile
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            var burst = def.GetModExtension<AbilityExtension_Burst>();
            var extra = (burst?.count ?? 1) - 1;
            var pool = AbilityEnergyUtility.SourceFor(this);
            for (var i = 0; i < extra; i++)
            {
                if (pool != null && burst.extraCost > 0f)
                {
                    if (!pool.CanPay(burst.extraCost))
                        break;
                    pool.Spend(burst.extraCost);
                }
                foreach (var target in targets)
                    ShootProjectile(target);
            }
        }
    }

    /// <summary>Ignite: heat burst at the target. Pawns and plants may catch fire (less likely further out), corpses always do, snow melts.</summary>
    public class Ability_Ignite : Ability
    {
        private const float Heat = 500f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            var map = pawn.Map;
            var radius = GetRadiusForPawn();
            foreach (var target in targets)
            {
                var center = target.Cell;
                GenTemperature.PushHeat(center, map, Heat);
                FleckMaker.Static(center.ToVector3Shifted(), map, FleckDefOf.ExplosionFlash, radius * 2f);

                foreach (var cell in GenRadial.RadialCellsAround(center, radius, true))
                {
                    if (!cell.InBounds(map))
                        continue;

                    var chance = 1f - cell.DistanceTo(center) / (radius + 1f);
                    if (map.snowGrid.GetDepth(cell) > 0f)
                    {
                        map.snowGrid.SetDepth(cell, 0f);
                        if (Rand.Chance(0.3f))
                            FleckMaker.ThrowSmoke(cell.ToVector3Shifted(), map, 1f);
                    }

                    var things = cell.GetThingList(map);
                    for (var i = things.Count - 1; i >= 0; i--)
                        TryIgnite(things[i], chance);
                }
            }
        }

        private void TryIgnite(Thing thing, float chance)
        {
            switch (thing)
            {
                case Corpse corpse:
                    corpse.TryAttachFire(0.5f, pawn);
                    break;
                case Pawn victim when !victim.RaceProps.IsMechanoid:
                    if (Rand.Chance(chance))
                        victim.TryAttachFire(0.4f, pawn);
                    break;
                case Plant plant:
                    if (Rand.Chance(plant.def.plant.IsTree ? chance * 0.5f : chance))
                        FireUtility.TryStartFireIn(plant.Position, plant.Map, 0.3f, pawn);
                    break;
            }
        }
    }

    /// <summary>
    /// Phase Strike: teleport (no line of sight needed) and hit everyone not of the caster's faction within the radius of the landing cell.
    /// Aimed at a pawn, the caster lands on the free cell next to it that is closest to where the caster stood.
    /// </summary>
    public class Ability_PhaseStrike : Ability
    {
        private const float CritChance = 0.1f;

        private IntVec3 LandingCell(IntVec3 aimed)
        {
            var map = pawn.Map;
            if (AbilityUtility.IsFreeStandCell(aimed, map))
                return aimed;

            var best = IntVec3.Invalid;
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var cell = aimed + offset;
                if (AbilityUtility.IsFreeStandCell(cell, map) && (!best.IsValid || cell.DistanceToSquared(pawn.Position) < best.DistanceToSquared(pawn.Position)))
                    best = cell;
            }
            return best;
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Thing == pawn || !LandingCell(target.Cell).IsValid)
            {
                if (showMessages)
                    Messages.Message("ABT_WarpInvalidDestination".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.ValidateTarget(target, showMessages);
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            var cell = LandingCell(targets[0].Cell);
            if (!cell.IsValid)
                return;

            AbilityUtility.Teleport(pawn, cell);
            var hit = AbilityUtility.BestMeleeHit(pawn);
            FleckMaker.Static(pawn.DrawPos, pawn.Map, FleckDefOf.PsycastAreaEffect, GetRadiusForPawn() * 2f);

            foreach (var victim in AbilityUtility.PawnsInRadius(cell, pawn.Map, GetRadiusForPawn()).ToList())
            {
                if (victim == pawn || victim.Faction == pawn.Faction && pawn.Faction != null)
                    continue;
                AbilityUtility.Strike(pawn, victim, hit.amount * Rand.Range(0.5f, 1.5f), hit.damageDef, hit.armorPenetration, CritChance);
            }
        }
    }

    /// <summary>Seismic Slash: a cutting wave along a line, hitting every pawn not of the caster's faction on it or one cell to either side.</summary>
    public class Ability_SeismicSlash : Ability
    {
        private const float DamageFactor = 0.7f;
        private const float HalfWidth = 1.5f;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            var map = pawn.Map;
            var from = pawn.Position.ToVector3Shifted();
            var to = targets[0].Cell.ToVector3Shifted();
            var hit = AbilityUtility.BestMeleeHit(pawn);
            var penetration = Mathf.Max(hit.PenetrationFor(DamageDefOf.Cut), def.GetModExtension<AbilityExtension_MinPenetration>()?.For() ?? 0f);

            foreach (var cell in GenSight.PointsOnLineOfSight(pawn.Position, targets[0].Cell))
                FleckMaker.ThrowDustPuffThick(cell.ToVector3Shifted(), map, 1f, new Color(0.7f, 0.6f, 0.5f));

            var middle = ((from + to) / 2f).ToIntVec3();
            var searchRadius = Mathf.Min(GenRadial.MaxRadialPatternRadius - 1f, (to - from).MagnitudeHorizontal() / 2f + HalfWidth + 1f);
            foreach (var victim in AbilityUtility.PawnsInRadius(middle, map, searchRadius).ToList())
            {
                if (victim == pawn || victim.Faction == pawn.Faction && pawn.Faction != null)
                    continue;
                if (DistanceToSegment(victim.Position.ToVector3Shifted(), from, to) > HalfWidth)
                    continue;

                AbilityUtility.Strike(pawn, victim, hit.amount * DamageFactor * Rand.Range(0.7f, 1.3f), DamageDefOf.Cut, penetration);
            }
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            ab.y = 0f;
            var lengthSq = ab.sqrMagnitude;
            var t = lengthSq < 0.001f ? 0f : Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSq);
            var closest = a + ab * t;
            return (point - closest).MagnitudeHorizontal();
        }
    }

    /// <summary>Create Light: places a light orb at the target. Casting again, anywhere, dismisses it.</summary>
    public class Ability_CreateLight : Ability
    {
        private Thing light;

        private bool LightActive => light is { Spawned: true, Destroyed: false };

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            if (LightActive)
            {
                FleckMaker.ThrowLightningGlow(light.DrawPos, light.Map, 1f);
                light.Destroy();
                light = null;
                return;
            }

            light = GenSpawn.Spawn(BOR_ArcaneDefOf.ABT_LightOrb, targets[0].Cell, pawn.Map);
            FleckMaker.ThrowLightningGlow(light.DrawPos, light.Map, 2f);
        }

        public override string GetDescriptionForPawn() =>
            base.GetDescriptionForPawn() + (LightActive ? "\n\n" + "ABT_LightActive".Translate().Colorize(ColoredText.SubtleGrayColor) : string.Empty);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref light, "light");
        }
    }

    /// <summary>
    /// Eye of the archotech: a golden light that hovers and breathes. Drawn as a wide halo, a brighter middle and a hot core
    /// (the glow shader is additive, so the layers stack into a solid light), with an occasional wisp of golden mist.
    /// </summary>
    public class Thing_ArchotechEye : ThingWithComps
    {
        private const float Hover = 0.35f;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (Graphic is not { } graphic)
                return;

            var t = Find.TickManager.TicksGame + thingIDNumber * 37;
            var pulse = 1f + 0.08f * Mathf.Sin(t * 0.05f);
            var pos = drawLoc;
            pos.z += Hover + 0.06f * Mathf.Sin(t * 0.03f);
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();

            var mat = graphic.MatSingle;
            var size = graphic.drawSize.x;
            Layer(pos, size * 2.4f * pulse, FadedMaterialPool.FadedVersionOf(mat, 0.35f));
            Layer(pos, size * 1.3f * pulse, FadedMaterialPool.FadedVersionOf(mat, 0.8f));
            Layer(pos, size * 0.7f, mat);
            Layer(pos, size * 0.45f, mat);
            Comps_PostDraw();
        }

        private static void Layer(Vector3 pos, float size, Material mat) =>
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size)), mat, 0);

        public override void TickRare()
        {
            base.TickRare();
            if (Spawned)
                BOR_AbilityVisuals.GoldenMist(Position, Map, 0.6f, 1);
        }
    }

    /// <summary>Psionic Barrier: after the cast the caster channels a projectile-stopping bubble. Any new order ends it.</summary>
    public class Ability_PsionicBarrier : Ability
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            // targetB carries the pool that pays for deflections (none: free), so a pawn-held barrier never drains apparel.
            var pool = AbilityEnergyUtility.SourceFor(this);
            var job = JobMaker.MakeJob(BOR_ArcaneDefOf.ABT_ChannelBarrier, LocalTargetInfo.Invalid, pool != null ? new LocalTargetInfo(pool.parent) : LocalTargetInfo.Invalid);
            job.count = Mathf.Max(60, GetDurationForPawn());
            job.playerForced = true;
            pawn.jobs.jobQueue.EnqueueFirst(job);
        }
    }

    /// <summary>
    /// The deflector bubble. The vanilla interceptor comp stops projectiles; <see cref="Blocks"/> also stops fire spews, beams
    /// and blasts that start outside the bubble (see DeflectorPatches). It has no graphic of its own, only the comp's bubble.
    /// </summary>
    public class Thing_PsionicBarrier : ThingWithComps
    {
        private static readonly List<Thing_PsionicBarrier> Active = new List<Thing_PsionicBarrier>();

        private static readonly HarmonyLib.AccessTools.FieldRef<CompProjectileInterceptor, int> LastInterceptTicks =
            HarmonyLib.AccessTools.FieldRefAccess<CompProjectileInterceptor, int>("lastInterceptTicks");
        private static readonly HarmonyLib.AccessTools.FieldRef<CompProjectileInterceptor, float> LastInterceptAngle =
            HarmonyLib.AccessTools.FieldRefAccess<CompProjectileInterceptor, float>("lastInterceptAngle");

        private CompProjectileInterceptor interceptor;

        // Spawned and Map only go by the map's index, which after loading another save points at that game's map.
        private Map spawnedMap;

        protected override void DrawAt(Vector3 drawLoc, bool flip = false) => Comps_PostDraw();

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            interceptor = GetComp<CompProjectileInterceptor>();
            spawnedMap = map;
            if (!Active.Contains(this))
                Active.Add(this);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Active.Remove(this);
            base.DeSpawn(mode);
        }

        /// <summary>
        /// True when <paramref name="cell"/> is inside an active deflector and <paramref name="source"/> is outside it.
        /// Called per damaged cell, so the common case (no deflector anywhere) exits on the first check.
        /// </summary>
        public static bool Blocks(Map map, IntVec3 source, IntVec3 cell)
        {
            if (Active.Count == 0 || map == null)
                return false;

            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var barrier = Active[i];
                if (!barrier.Spawned || Find.Maps?.Contains(barrier.spawnedMap) != true)
                {
                    // Left over from a map or game that was unloaded without despawning.
                    Active.RemoveAt(i);
                    continue;
                }
                if (barrier.spawnedMap != map || barrier.interceptor is not { Active: true } comp)
                    continue;

                var radiusSq = comp.Props.radius * comp.Props.radius;
                if ((cell - barrier.Position).LengthHorizontalSquared > radiusSq || (source - barrier.Position).LengthHorizontalSquared <= radiusSq)
                    continue;

                // Same flash and energy drain as a stopped projectile (the channel job drains once per intercept tick).
                LastInterceptAngle(comp) = source.ToVector3Shifted().AngleToFlat(barrier.TrueCenter());
                LastInterceptTicks(comp) = Find.TickManager.TicksGame;
                return true;
            }
            return false;
        }
    }

    public class JobDriver_ChannelBarrier : JobDriver
    {
        private const float EnergyPerDeflection = 3f;
        private const float MinEnergyFraction = 0.01f;

        private static readonly HarmonyLib.AccessTools.FieldRef<CompProjectileInterceptor, int> LastInterceptTicks =
            HarmonyLib.AccessTools.FieldRefAccess<CompProjectileInterceptor, int>("lastInterceptTicks");

        private Thing barrier;
        private int seenInterceptTick = -1;

        /// <summary>Each tick that the barrier stopped something costs energy; it collapses below 1% of the pool.</summary>
        private void DrainForDeflections()
        {
            if (barrier?.TryGetComp<CompProjectileInterceptor>() is not { } interceptor || job.targetB.Thing?.TryGetComp<CompAbilityEnergy>() is not { } pool)
                return;

            var last = LastInterceptTicks(interceptor);
            if (last > seenInterceptTick && last >= startTick)
            {
                seenInterceptTick = last;
                pool.Spend(EnergyPerDeflection);
            }

            if (pool.EnergyFraction < MinEnergyFraction)
                EndJobWith(JobCondition.Succeeded);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => pawn.Downed);
            AddFinishAction(_ =>
            {
                if (barrier is { Destroyed: false })
                    barrier.Destroy();
                barrier = null;
            });

            var channel = ToilMaker.MakeToil("ChannelBarrier");
            channel.initAction = () =>
            {
                pawn.pather.StopDead();
                if (barrier == null || barrier.Destroyed)
                {
                    barrier = GenSpawn.Spawn(BOR_ArcaneDefOf.ABT_PsionicBarrierField, pawn.Position, pawn.Map);
                }
            };
            channel.tickAction = () =>
            {
                if (pawn.IsHashIntervalTick(20))
                    BOR_AbilityVisuals.GoldenMist(pawn.Position, pawn.Map, 2.5f, 2, edgeOnly: true);
                DrainForDeflections();
            };
            channel.defaultCompleteMode = ToilCompleteMode.Delay;
            channel.defaultDuration = job.count;
            channel.handlingFacing = true;
            yield return channel;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref barrier, "barrier");
            Scribe_Values.Look(ref seenInterceptTick, "seenInterceptTick", -1);
        }
    }

    /// <summary>Resurrection: only a fresh humanlike corpse is a valid target. The ritual itself is a <see cref="Field_ResurrectionRitual"/>.</summary>
    public class Ability_Resurrection : Ability
    {
        public override bool CanAutoCast => false;

        public static bool IsRevivable(Corpse corpse) =>
            corpse?.InnerPawn is { } inner && inner.RaceProps.Humanlike && corpse.GetRotStage() == RotStage.Fresh && HasHeadAndBrain(inner);

        /// <summary>The nanites restart a body, they cannot rebuild a missing head or brain.</summary>
        public static bool HasHeadAndBrain(Pawn pawn) =>
            pawn.health.hediffSet.HasHead && pawn.health.hediffSet.GetBrain() != null;

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            var corpse = target.Thing as Corpse ?? target.Cell.GetFirstThing<Corpse>(pawn.Map);
            if (corpse == null || !corpse.InnerPawn.RaceProps.Humanlike)
            {
                if (showMessages)
                    Messages.Message("ABT_ResurrectionNeedsCorpse".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!HasHeadAndBrain(corpse.InnerPawn))
            {
                if (showMessages)
                    Messages.Message("ABT_ResurrectionHeadless".Translate(corpse.InnerPawn.LabelShort), corpse, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (!IsRevivable(corpse))
            {
                if (showMessages)
                    Messages.Message("ABT_ResurrectionExpired".Translate(corpse.InnerPawn.LabelShort), corpse, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.ValidateTarget(target, showMessages);
        }
    }

    /// <summary>Psionic Storm: the caster takes off, circles the target while raining psionic bolts, lands there and is left exhausted.</summary>
    public class Ability_PsionicStorm : Ability
    {
        public override bool CanAutoCast => false;

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!target.Cell.Standable(pawn.Map))
            {
                if (showMessages)
                    Messages.Message("ABT_WarpInvalidDestination".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.ValidateTarget(target, showMessages);
        }

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            var map = pawn.Map;
            var extension = def.GetModExtension<AbilityExtension_Storm>();
            var flyer = (Flyer_PsionicStorm)PawnFlyer.MakeFlyer(extension.flyer, pawn, targets[0].Cell, null, null);
            if (flyer == null)
                return;

            flyer.ability = this;
            flyer.radius = GetRadiusForPawn();
            flyer.bolt = extension.bolt;
            GenSpawn.Spawn(flyer, pawn.Position, map);
        }
    }

    public class AbilityExtension_Storm : DefModExtension
    {
        public ThingDef flyer;
        public ThingDef bolt;
    }

    public class Flyer_PsionicStorm : AbilityPawnFlyer
    {
        public float radius = 5f;
        public ThingDef bolt;

        private int nextBoltTick;

        private const float Passes = 3f;
        private const float CircleShare = 0.85f;
        private const float Altitude = 1.6f;
        private static readonly IntRange BoltInterval = new IntRange(10, 40);

        private Vector3 Center => DestinationCell.ToVector3Shifted();

        private Vector3 RingPoint(float t)
        {
            var angle = t / CircleShare * Passes * 360f;
            return Center + Vector3Utility.HorizontalVectorFromAngle(angle) * radius;
        }

        /// <summary>Where the caster is at this tick: circling the area, then coming down in its middle.</summary>
        private void FlightPosition(out Vector3 ground, out float height)
        {
            var t = ticksFlying / (float)Mathf.Max(1, ticksFlightTime);
            if (t < CircleShare)
            {
                var climb = Mathf.Clamp01(t / 0.1f);
                ground = Vector3.Lerp(startVec, RingPoint(t), climb);
                height = Altitude * climb;
            }
            else
            {
                var land = (t - CircleShare) / (1f - CircleShare);
                ground = Vector3.Lerp(RingPoint(CircleShare), Center, land);
                height = Altitude * (1f - land);
            }
        }

        protected override bool CustomRecomputePosition()
        {
            FlightPosition(out var ground, out var height);
            GroundPos = ground;
            EffectivePos = ground + new Vector3(0f, 0f, height);
            EffectiveHeight = height / Altitude;
            return true;
        }

        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (Destroyed || bolt == null || ticksFlying >= ticksFlightTime * CircleShare)
                return;

            if (ticksFlying >= nextBoltTick)
            {
                nextBoltTick = ticksFlying + BoltInterval.RandomInRange;
                FireBolt();
            }

            // A trail of golden mist behind the caster as it circles.
            if (ticksFlying % 6 == 0)
            {
                FlightPosition(out var ground, out _);
                var cell = ground.ToIntVec3();
                if (cell.InBounds(Map))
                    BOR_AbilityVisuals.GoldenMist(cell, Map, 0.3f, 1);
            }
        }

        private void FireBolt()
        {
            var cell = DestinationCell + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(radius))];
            if (!cell.InBounds(Map))
                return;

            // EffectivePos is only refreshed when the flyer is drawn, which can lag behind ticks; work the position out here.
            FlightPosition(out var ground, out var height);
            var origin = ground + new Vector3(0f, 0f, height);
            var projectile = (Projectile)GenSpawn.Spawn(bolt, ground.ToIntVec3().ClampInsideMap(Map), Map);
            if (projectile is AbilityProjectile abilityProjectile)
                abilityProjectile.ability = ability;
            projectile.Launch(FlyingPawn, origin, cell, cell, ProjectileHitFlags.All);
        }

        protected override void RespawnPawn()
        {
            var pawn = FlyingPawn;
            base.RespawnPawn();
            pawn?.health.AddHediff(BOR_ArcaneDefOf.ABT_PsionicExhaustionHediff);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref radius, "radius", 5f);
            Scribe_Defs.Look(ref bolt, "bolt");
            Scribe_Values.Look(ref nextBoltTick, "nextBoltTick");
        }
    }
}
