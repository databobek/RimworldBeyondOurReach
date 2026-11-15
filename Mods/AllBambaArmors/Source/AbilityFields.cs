using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

using AllBambaArmors;
using AbilityUtility = AllBambaArmors.AbilityUtility;

namespace BeyondOurReach
{
    /// <summary>Timing for an <see cref="AbilityField"/>, set on its ThingDef.</summary>
    public class AbilityFieldProps : DefModExtension
    {
        /// <summary>Ticks before the first pulse. <see cref="AbilityField.DelayTick"/> runs every tick until then.</summary>
        public int delayTicks;
        /// <summary>Ticks of pulsing after the delay. The field finishes and despawns when this runs out.</summary>
        public int durationTicks = 60;
        /// <summary>Ticks between pulses. The first pulse fires on the first tick after the delay.</summary>
        public int intervalTicks = 60;
        /// <summary>End early when the caster dies or is downed.</summary>
        public bool endsWithCaster;
        public SoundDef sustainer;
    }

    /// <summary>
    /// An invisible thing that sits on a cell and acts on a schedule: delay, then pulses, then finish.
    /// Every lingering area ability uses it, so per-tick work is one counter check and the real work runs only on pulse ticks.
    /// </summary>
    public abstract class AbilityField : ThingWithComps
    {
        protected Pawn caster;
        protected Faction casterFaction;
        protected LocalTargetInfo target;
        protected float radius;
        protected float power;
        protected int age;
        protected int pulses;

        private Sustainer sustainer;

        protected AbilityFieldProps Props => def.GetModExtension<AbilityFieldProps>() ?? DefaultProps;
        private static readonly AbilityFieldProps DefaultProps = new AbilityFieldProps();

        public void Setup(Pawn caster, LocalTargetInfo target, float radius, float power)
        {
            this.caster = caster;
            casterFaction = caster?.Faction;
            this.target = target;
            this.radius = radius;
            this.power = power;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
                OnStart();
        }

        protected override void Tick()
        {
            base.Tick();
            if (Destroyed)
                return;

            age++;
            if (Props.endsWithCaster && (caster == null || caster.Dead || caster.Downed))
            {
                Destroy();
                return;
            }

            if (Props.sustainer != null)
            {
                if (sustainer == null || sustainer.Ended)
                    sustainer = Props.sustainer.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
                sustainer?.Maintain();
            }

            if (age <= Props.delayTicks)
            {
                DelayTick(age);
                return;
            }

            var active = age - Props.delayTicks;
            if ((active - 1) % Mathf.Max(1, Props.intervalTicks) == 0)
            {
                Pulse(pulses);
                pulses++;
            }

            if (!Destroyed && active >= Props.durationTicks)
            {
                Finish();
                if (!Destroyed)
                    Destroy();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            sustainer?.End();
            sustainer = null;
            base.DeSpawn(mode);
        }

        protected virtual void OnStart() { }
        protected virtual void DelayTick(int tick) { }
        protected abstract void Pulse(int index);
        protected virtual void Finish() { }

        protected IntVec3 RandomCellInRadius()
        {
            for (var i = 0; i < 10; i++)
            {
                var cell = Position + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(radius))];
                if (cell.InBounds(Map))
                    return cell;
            }
            return Position;
        }

        protected bool IsFriendlyToCaster(Pawn pawn) =>
            caster != null ? AbilityUtility.IsFriendly(caster, pawn) : pawn.Faction == casterFaction || !pawn.HostileTo(casterFaction);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster", true);
            Scribe_References.Look(ref casterFaction, "casterFaction");
            Scribe_TargetInfo.Look(ref target, "target");
            Scribe_Values.Look(ref radius, "radius");
            Scribe_Values.Look(ref power, "power");
            Scribe_Values.Look(ref age, "age");
            Scribe_Values.Look(ref pulses, "pulses");
        }
    }

    /// <summary>
    /// Healing Circle: a ring drawn on the ground; each pulse heals one injury on every friendly pawn inside
    /// and sends a ripple out from the center.
    /// </summary>
    [StaticConstructorOnStartup]
    public class Field_HealingCircle : AbilityField
    {
        private static readonly Material RingMat = MaterialPool.MatFrom("Things/Abilities/RestorationRing", ShaderDatabase.MoteGlow, new Color(1f, 0.82f, 0.35f));

        private const int FadeInTicks = 30;
        private const int FadeOutTicks = 60;
        private const int RippleTicks = 45;

        private int lastPulseAge = -999;

        protected override void Pulse(int index)
        {
            lastPulseAge = age;
            foreach (var pawn in AbilityUtility.PawnsInRadius(Position, Map, radius).ToList())
            {
                if (IsFriendlyToCaster(pawn))
                    AbilityUtility.HealInjuries(pawn, 1, power);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var total = Props.delayTicks + Props.durationTicks;
            var alpha = Mathf.Clamp01(age / (float)FadeInTicks) * Mathf.Clamp01((total - age) / (float)FadeOutTicks);
            alpha *= 0.8f + 0.1f * Mathf.Sin(age * 0.08f);

            var pos = Position.ToVector3Shifted();
            pos.y = AltitudeLayer.MoteLow.AltitudeFor();
            var size = radius * 2f + 0.5f;
            DrawRing(pos, size, age * 0.2f, alpha);

            var sincePulse = age - lastPulseAge;
            if (sincePulse >= 0 && sincePulse < RippleTicks)
            {
                var t = sincePulse / (float)RippleTicks;
                DrawRing(pos, size * t, -age * 0.2f, alpha * (1f - t));
            }
        }

        private static void DrawRing(Vector3 pos, float size, float angle, float alpha)
        {
            if (size <= 0.05f || alpha <= 0.01f)
                return;
            var matrix = Matrix4x4.TRS(pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(size, 1f, size));
            Graphics.DrawMesh(MeshPool.plane10, matrix, FadedMaterialPool.FadedVersionOf(RingMat, alpha), 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastPulseAge, "lastPulseAge", -999);
        }
    }

    /// <summary>A normal blast (Bomb damage, set in XML) that also plays vanilla's vaporize heatwave when it goes off.</summary>
    public class DamageWorker_VaporizeBlast : DamageWorker_AddInjury
    {
        private const float VanillaVaporizeRadius = 2.9f;

        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            base.ExplosionStart(explosion, cellsToAffect);
            BOR_AbilityVisuals.VaporizeBurst(explosion.Position, explosion.Map, Mathf.Clamp(explosion.radius / VanillaVaporizeRadius, 0.5f, 4f), false);
        }
    }

    /// <summary>Holy Wrath: a warning delay, then strikes that hurt and stun enemies while blessing the caster's faction.</summary>
    public class Field_HolyWrath : AbilityField
    {
        private const float StrikeRadius = 3f;
        private const int StunTicks = 90;

        protected override void DelayTick(int tick)
        {
            // Golden mist gathers over the marked area during the warning delay.
            if (tick % 12 == 1)
                BOR_AbilityVisuals.GoldenMist(Position, Map, radius, 3);
        }

        protected override void Pulse(int index)
        {
            if (index == 0)
                BlessAllies();

            var cell = RandomCellInRadius();
            AbilityUtility.SkyBolt(cell, Map);
            BOR_AbilityVisuals.VaporizeBurst(cell, Map, StrikeRadius / 2.9f, index % 2 == 0);

            foreach (var pawn in AbilityUtility.PawnsInRadius(cell, Map, StrikeRadius).ToList())
            {
                if (caster == null ? !pawn.HostileTo(casterFaction) : !pawn.HostileTo(caster))
                    continue;

                var amount = power * Rand.Range(0.8f, 1.2f) * (pawn.IsMutant ? 2f : 1f);
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, amount, 0.3f, -1f, caster));
                if (!pawn.Dead)
                    pawn.stances?.stunner.StunFor(StunTicks, caster);
            }
        }

        private void BlessAllies()
        {
            var might = BOR_ArcaneDefOf.ABT_BestowMightHediff;
            foreach (var pawn in AbilityUtility.PawnsInRadius(Position, Map, radius).ToList())
            {
                if (pawn.Faction == null || pawn.Faction != casterFaction)
                    continue;

                pawn.health.AddHediff(BOR_ArcaneDefOf.ABT_HolyWardHediff);
                pawn.health.AddHediff(might);
                AbilityUtility.HealInjuries(pawn, 3, 10f);
            }
        }
    }

    /// <summary>Sunfire: launches a beam of light every pulse; each beam sweeps a short path and burns what it crosses.</summary>
    public class Field_Sunfire : AbilityField
    {
        protected override void Pulse(int index)
        {
            var start = RandomCellInRadius().ToVector3Shifted();
            var end = start + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0f, 360f)) * Rand.Range(2f, 4f);

            var beam = (Thing_SunfireBeam)ThingMaker.MakeThing(BOR_ArcaneDefOf.ABT_SunfireBeam);
            beam.Setup(caster, start, end, power);
            GenSpawn.Spawn(beam, start.ToIntVec3(), Map);
            beam.StartStrike();
        }
    }

    /// <summary>One moving beam of Sunfire. Reuses the vanilla orbital beam visual.</summary>
    public class Thing_SunfireBeam : OrbitalStrike
    {
        private Vector3 start;
        private Vector3 end;
        private float damage;
        private IntVec3 lastCell = IntVec3.Invalid;

        private const int DamageInterval = 5;
        private const float BuildingFactor = 4f;
        private const float FireChance = 0.02f;

        public void Setup(Pawn caster, Vector3 start, Vector3 end, float damage)
        {
            instigator = caster;
            this.start = start;
            this.end = end;
            this.damage = damage;
            duration = Rand.Range(40, 60);
        }

        private float Progress => Mathf.Clamp01(TicksPassed / (float)Mathf.Max(1, duration));

        public override Vector3 DrawPos => Vector3.Lerp(start, end, Progress);

        protected override void Tick()
        {
            var cell = DrawPos.ToIntVec3();
            if (cell.InBounds(Map) && cell != Position)
                Position = cell;

            if (TicksPassed % DamageInterval == 0 || cell != lastCell)
            {
                lastCell = cell;
                BurnCell(cell);
            }

            GroundImpact();
            base.Tick();
        }

        /// <summary>Where the lance touches the ground: a hot glow, sparks and a little smoke, cheap enough for 13 beams.</summary>
        private void GroundImpact()
        {
            var ground = DrawPos;
            if (TicksPassed % 3 == 0)
                FleckMaker.ThrowFireGlow(ground, Map, 1.3f);
            if (TicksPassed % 5 == 0)
                FleckMaker.ThrowMicroSparks(ground, Map);
            if (TicksPassed % 9 == 0 && ABT_DefOf.Fleck_RadialSparks != null)
                FleckMaker.Static(ground, Map, ABT_DefOf.Fleck_RadialSparks, 0.6f);
            if (TicksPassed % 12 == 0)
                FleckMaker.ThrowSmoke(ground, Map, 0.9f);
        }

        private void BurnCell(IntVec3 cell)
        {
            if (!cell.InBounds(Map))
                return;

            FleckMaker.ThrowMicroSparks(cell.ToVector3Shifted(), Map);
            var things = cell.GetThingList(Map);
            for (var i = things.Count - 1; i >= 0; i--)
            {
                var thing = things[i];
                if (thing == this || thing is Mote || thing.def.category == ThingCategory.Filth)
                    continue;

                if (thing is Pawn pawn)
                    pawn.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage * Rand.Range(0.7f, 1.3f), 0.1f, -1f, instigator));
                else if (thing is Building || thing.def.useHitPoints && thing.def.category == ThingCategory.Building)
                {
                    thing.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage * BuildingFactor, 0f, -1f, instigator));
                    if (Rand.Chance(FireChance))
                        FireUtility.TryStartFireIn(cell, Map, 0.3f, instigator);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref start, "start");
            Scribe_Values.Look(ref end, "end");
            Scribe_Values.Look(ref damage, "damage");
        }
    }

    /// <summary>
    /// Explosion!: magic rings gather for the delay, then four expanding waves of bomb blasts out to the full radius.
    /// It hits everything, the caster's side included.
    /// </summary>
    public class Field_ArcaneExplosion : AbilityField
    {
        private const int Waves = 4;
        private const float BlastRadius = 3f;
        private const float HeatPerWave = 600f;

        protected override void DelayTick(int tick)
        {
            if (tick % 12 != 1)
                return;

            // A ring of golden mist closes in on the target while the glow at its center builds.
            var progress = tick / (float)Mathf.Max(1, Props.delayTicks);
            BOR_AbilityVisuals.GoldenMist(Position, Map, radius * (1.05f - progress * 0.85f), 6, edgeOnly: true);
            if (tick % 24 == 1)
                FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 2f + 6f * progress);
        }

        protected override void Pulse(int index)
        {
            if (index >= Waves)
                return;

            var ringRadius = radius * (index + 1) / Waves;
            var instigator = (Thing)caster;

            var blast = BOR_ArcaneDefOf.ABT_VaporizeBlast;
            if (index == 0)
            {
                GenExplosion.DoExplosion(Position, Map, BlastRadius, blast, instigator, BlastDamage());
                BOR_AbilityVisuals.VaporizeBurst(Position, Map, radius / 2.9f, true);
            }

            var points = Mathf.Max(4, Mathf.RoundToInt(ringRadius * 1.2f));
            var offset = Rand.Range(0f, 360f);
            for (var i = 0; i < points; i++)
            {
                var cell = (Position.ToVector3Shifted() + Vector3Utility.HorizontalVectorFromAngle(offset + 360f * i / points) * ringRadius).ToIntVec3();
                if (cell.InBounds(Map))
                    GenExplosion.DoExplosion(cell, Map, BlastRadius, blast, instigator, BlastDamage(), doSoundEffects: i % 3 == 0, screenShakeFactor: 0.5f);
            }

            var flame = Mathf.RoundToInt(power * Rand.Range(0.25f, 0.35f));
            foreach (var pawn in AbilityUtility.PawnsInRadius(Position, Map, ringRadius).ToList())
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Flame, flame, 0f, -1f, instigator));

            GenTemperature.PushHeat(Position, Map, HeatPerWave);
        }

        private int BlastDamage() => Mathf.RoundToInt(power * Rand.Range(0.7f, 0.9f));
    }

    /// <summary>Resurrection: a ritual on the corpse; when it ends the pawn returns with lasting weakness, and undead nearby are seared.</summary>
    public class Field_ResurrectionRitual : AbilityField
    {
        private const float ShockwaveRadius = 12f;
        private const float MaxSkillLoss = 0.35f;
        private const float PsychosisChance = 0.15f;

        private Corpse Corpse => target.Thing as Corpse;

        protected override void Pulse(int index)
        {
            BOR_AbilityVisuals.GoldenMist(Position, Map, 0.8f, 2);
            FleckMaker.ThrowLightningGlow(Position.ToVector3Shifted(), Map, 0.5f + index * 0.02f);
        }

        protected override void Finish()
        {
            var corpse = Corpse;
            if (corpse == null || !corpse.Spawned || corpse.Destroyed || !Ability_Resurrection.IsRevivable(corpse))
            {
                Messages.Message("ABT_ResurrectionFailed".Translate(), new TargetInfo(Position, Map), MessageTypeDefOf.NegativeEvent);
                return;
            }

            var pawn = corpse.InnerPawn;
            if (!ResurrectionUtility.TryResurrect(pawn))
                return;

            pawn.health.AddHediff(BOR_ArcaneDefOf.ABT_ResurrectedHediff);
            if (Rand.Chance(PsychosisChance) && DefDatabase<HediffDef>.GetNamedSilentFail("ResurrectionPsychosis") is { } psychosis)
                pawn.health.AddHediff(psychosis, pawn.health.hediffSet.GetBrain());

            if (pawn.skills != null)
            {
                foreach (var skill in pawn.skills.skills)
                    skill.Level = Mathf.RoundToInt(skill.Level * (1f - Rand.Range(0f, MaxSkillLoss)));
            }

            caster?.health.AddHediff(BOR_ArcaneDefOf.ABT_ResurrectionDrainHediff);

            // Royalty sound: null without the DLC.
            SoundDefOf.PsycastPsychicPulse?.PlayOneShot(new TargetInfo(Position, Map));
            BOR_AbilityVisuals.GoldenMist(Position, Map, 2f, 8);
            foreach (var undead in AbilityUtility.PawnsInRadius(Position, Map, ShockwaveRadius).ToList())
            {
                if (undead.IsMutant)
                    undead.TakeDamage(new DamageInfo(DamageDefOf.Burn, Rand.Range(10f, 20f), 1f, -1f, caster));
            }

            Messages.Message("ABT_ResurrectionSucceeded".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.PositiveEvent);
        }
    }

    /// <summary>
    /// Chain Lightning: the first strike, then waves of forks. Each struck point forks up to twice toward things further along the bolt.
    /// Lives only a few ticks, so the hit list is not saved: a save mid-chain just ends the chain.
    /// </summary>
    public class Field_ChainLightning : AbilityField
    {
        private const int Forks = 2;
        private const float HopDistance = 4f;
        private const float DamageDropPerWave = 0.2f;

        private List<(Vector3 from, Thing thing)> frontier;
        private HashSet<Thing> struck;

        protected override void OnStart()
        {
            struck = new HashSet<Thing> { caster };
            frontier = new List<(Vector3, Thing)>();
            if (target.Thing != null)
                frontier.Add((caster?.DrawPos ?? Position.ToVector3Shifted(), target.Thing));
        }

        protected override void Pulse(int wave)
        {
            if (frontier == null || frontier.Count == 0)
            {
                Destroy();
                return;
            }

            var damage = power * Mathf.Max(0.2f, 1f - DamageDropPerWave * wave);
            var next = new List<(Vector3, Thing)>();

            foreach (var (from, thing) in frontier)
            {
                // The same thing is never hit twice by one cast, even if two forks picked it.
                if (thing.Destroyed || !thing.Spawned || !struck.Add(thing))
                    continue;

                var to = thing.DrawPos;
                AbilityUtility.ArcBetween(from, to, Map);
                Zap(thing, damage);
                StrayBolts(to, to - from);

                var direction = (to - from).normalized;
                foreach (var forkTarget in FindForks(to, direction))
                    next.Add((to, forkTarget));
            }

            SoundDefOf.Thunder_OnMap.PlayOneShot(new TargetInfo(Position, Map));
            frontier = next;
        }

        private void Zap(Thing thing, float damage)
        {
            if (thing is Building && thing.Faction != null)
                damage *= 2f;
            thing.TakeDamage(new DamageInfo(BOR_ArcaneDefOf.ABT_Lightning, damage, 0.5f, -1f, caster));
        }

        private IEnumerable<Thing> FindForks(Vector3 at, Vector3 direction)
        {
            var center = (at + direction * HopDistance).ToIntVec3();
            if (!center.InBounds(Map))
                center = at.ToIntVec3();

            return GenRadial.RadialDistinctThingsAround(center, Map, HopDistance, true)
                .Where(t => (t is Pawn p && !p.Dead || t is Building && t.def.useHitPoints) && !struck.Contains(t))
                .InRandomOrder()
                .Take(Forks)
                .ToList();
        }

        private void StrayBolts(Vector3 at, Vector3 direction)
        {
            var count = Rand.RangeInclusive(1, 3);
            for (var i = 0; i < count; i++)
            {
                var angle = direction.AngleFlat() + Rand.Range(-90f, 90f);
                var cell = (at + Vector3Utility.HorizontalVectorFromAngle(angle) * Rand.Range(1f, 6f)).ToIntVec3();
                if (!cell.InBounds(Map))
                    continue;
                AbilityUtility.ArcBetween(at, cell.ToVector3Shifted(), Map);
                GenExplosion.DoExplosion(cell, Map, 1f, BOR_ArcaneDefOf.ABT_Lightning, caster, Rand.Range(4, 8), doSoundEffects: false);
            }
        }
    }
}
