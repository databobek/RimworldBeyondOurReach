using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using VEF.Hediffs;
using Verse;
using Verse.AI;
using AllBambaArmors;
using AbilityUtility = AllBambaArmors.AbilityUtility;

namespace BeyondOurReach
{
    public class HediffCompProperties_AbilityShield : HediffCompProperties_Shield
    {
        /// <summary>Recasting on a shielded pawn adds a fresh shield's worth of absorption, up to this cap.</summary>
        public float maxStackedEnergy = 150f;

        public HediffCompProperties_AbilityShield()
        {
            compClass = typeof(HediffComp_AbilityShield);
        }
    }

    /// <summary>
    /// A one-shot damage shield: it absorbs up to <c>maxEnergy</c> damage, lets the overflow through, and is gone once broken.
    /// Built on VEF's shield comp, which already splits a hit between shield and pawn.
    /// </summary>
    public class HediffComp_AbilityShield : HediffComp_Shield
    {
        private bool broken;

        private HediffCompProperties_AbilityShield ShieldProps => (HediffCompProperties_AbilityShield)props;

        // VEF calls PreApplyDamage while iterating the hediff list, so breaking must not remove the hediff right away.
        public override bool CompShouldRemove => broken || base.CompShouldRemove;

        public override bool ShieldActive => !broken && energy > 0f;

        protected override void Break()
        {
            base.Break();
            broken = true;
        }

        // VEF's version zeroes the energy in Break() before computing the overflow, so the whole hit passes through.
        protected override bool AbsorbDamage(ref DamageInfo dinfo)
        {
            var loss = dinfo.Amount * Props.energyLossPerDamage;
            if (loss < energy)
            {
                energy -= loss;
                dinfo.SetAmount(0f);
                return true;
            }

            var absorbedDamage = energy / Props.energyLossPerDamage;
            Break();
            dinfo.SetAmount(dinfo.Amount - absorbedDamage);
            return false;
        }

        // The ability shield never recharges; VEF's base would refill it after rechargeDelay.
        protected override void Reset() { }

        public override void CompPostMerged(Hediff other)
        {
            base.CompPostMerged(other);
            energy = Mathf.Min(energy + Props.maxEnergy, ShieldProps.maxStackedEnergy);
            broken = false;
        }

        public override string CompLabelInBracketsExtra => energy.ToString("0");

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref broken, "broken");
        }
    }

    public class HediffCompProperties_GrantAbilities : HediffCompProperties
    {
        public List<VEF.Abilities.AbilityDef> abilities = new List<VEF.Abilities.AbilityDef>();

        public HediffCompProperties_GrantAbilities()
        {
            compClass = typeof(HediffComp_GrantAbilities);
        }
    }

    /// <summary>
    /// Gives the pawn VEF abilities while it has this hediff, and takes them back when the hediff goes.
    /// Granting is re-checked once per session, so a comp added to an existing hediff by a patch still works on old saves.
    /// </summary>
    public class HediffComp_GrantAbilities : HediffComp
    {
        private bool granted;

        private HediffCompProperties_GrantAbilities Props => (HediffCompProperties_GrantAbilities)props;

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            Grant();
        }

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!granted && Pawn.IsHashIntervalTick(250, delta))
                Grant();
        }

        private void Grant()
        {
            if (Pawn.GetComp<VEF.Abilities.CompAbilities>() is not { } comp)
                return;
            foreach (var ability in Props.abilities)
                comp.GiveAbility(ability);
            granted = true;
        }

        // Only abilities the pawn holds itself; the same ability granted by worn apparel stays.
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            Pawn.GetComp<VEF.Abilities.CompAbilities>()?.LearnedAbilities.RemoveAll(a => a.holder == Pawn && Props.abilities.Contains(a.def));
        }
    }

    public class HediffCompProperties_HealOverTime : HediffCompProperties
    {
        public int intervalTicks = 300;
        public int injuries = 1;
        public float amount = 4f;

        public HediffCompProperties_HealOverTime()
        {
            compClass = typeof(HediffComp_HealOverTime);
        }
    }

    /// <summary>Heals the worst non-permanent injuries on an interval. Scars, diseases and missing parts are left alone.</summary>
    public class HediffComp_HealOverTime : HediffComp
    {
        private HediffCompProperties_HealOverTime Props => (HediffCompProperties_HealOverTime)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (Pawn.IsHashIntervalTick(Props.intervalTicks, delta))
                AbilityUtility.HealInjuries(Pawn, Props.injuries, Props.amount);
        }
    }

    public class HediffCompProperties_AllyAura : HediffCompProperties
    {
        public int intervalTicks = 300;
        public float radius = 15f;
        public HediffDef buff;
        public float severityPerPulse = 1f;
        public float maxSeverity = 4f;
        public bool humanlikeOnly = true;
        public float inspirationChance;
        public List<InspirationDef> inspirations = new List<InspirationDef>();
        /// <summary>Energy per pulse from the caster's pool. The aura ends when it cannot be paid.</summary>
        public float energyPerPulse;

        public HediffCompProperties_AllyAura()
        {
            compClass = typeof(HediffComp_AllyAura);
        }
    }

    /// <summary>
    /// Every interval, stacks a buff on the caster's faction pawns in range. Loops only the faction's spawned pawns, not the whole map.
    /// Ends itself when the caster is downed or out of energy.
    /// </summary>
    public class HediffComp_AllyAura : HediffComp, IAbilityEnergyUser
    {
        private bool outOfEnergy;
        private AbilityEnergyLink energy = new AbilityEnergyLink();

        public AbilityEnergyLink Energy => energy;

        private HediffCompProperties_AllyAura Props => (HediffCompProperties_AllyAura)props;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Deep.Look(ref energy, "energy");
            energy ??= new AbilityEnergyLink();
        }

        public override bool CompShouldRemove => outOfEnergy || Pawn.Downed || base.CompShouldRemove;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!Pawn.Spawned || Pawn.Faction == null || !Pawn.IsHashIntervalTick(Props.intervalTicks, delta))
                return;

            if (!AbilityEnergyUtility.TryPay(energy.Pool(Pawn), Props.energyPerPulse))
            {
                outOfEnergy = true;
                Messages.Message("ABT_AuraOutOfEnergy".Translate(Pawn.LabelShort, parent.LabelCap), Pawn, MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            var radiusSq = Props.radius * Props.radius;
            var allies = Pawn.Map.mapPawns.SpawnedPawnsInFaction(Pawn.Faction);
            for (var i = 0; i < allies.Count; i++)
            {
                var ally = allies[i];
                if (ally.Dead || Props.humanlikeOnly && !ally.RaceProps.Humanlike || (ally.Position - Pawn.Position).LengthHorizontalSquared > radiusSq)
                    continue;

                var buff = ally.health.hediffSet.GetFirstHediffOfDef(Props.buff) ?? ally.health.AddHediff(Props.buff);
                buff.Severity = Mathf.Min(buff.Severity + Props.severityPerPulse, Props.maxSeverity);

                if (Props.inspirationChance > 0f && Props.inspirations.Count > 0 && Rand.Chance(Props.inspirationChance) && ally.mindState?.inspirationHandler is { Inspired: false } handler)
                    handler.TryStartInspiration(Props.inspirations.RandomElement(), "ABT_InspiredByHymn".Translate(Pawn.LabelShort));
            }

            FleckMaker.ThrowMetaIcon(Pawn.Position, Pawn.Map, FleckDefOf.Heart);
        }
    }

    public class HediffCompProperties_TechnoBit : HediffCompProperties_Draw
    {
        public ThingDef laser;
        public ThingDef heavyLaser;
        public int laserInterval = 45;
        public int heavyLaserInterval = 180;

        public float repairRadius = 8f;
        public IntRange repairAmount = new IntRange(8, 12);
        public int repairIntervalDrafted = 160;
        public int repairIntervalUndrafted = 600;

        public HediffDef workBoost;
        public int workBoostInterval = 600;
        public float workDistance = 2f;

        /// <summary>Energy per shot, repair or assist, from the caster's pool (if any).</summary>
        public float energyPerAction = 1f;
        /// <summary>Pool fraction needed before the orb shoots or repairs, and before it assists work.</summary>
        public float minEnergyToFight = 0.1f;
        public float minEnergyToAssist = 0.9f;

        public HediffCompProperties_TechnoBit()
        {
            compClass = typeof(HediffComp_TechnoBit);
        }
    }

    /// <summary>
    /// A small orb next to the caster: shoots what the drafted caster is aiming at, repairs nearby buildings and mechs,
    /// and boosts manipulation while the caster crafts or builds. The repair part has its own on/off gizmo.
    /// </summary>
    public class HediffComp_TechnoBit : HediffComp_Draw, IAbilityEnergyUser
    {
        private bool repairEnabled = true;
        private AbilityEnergyLink energy = new AbilityEnergyLink();

        public AbilityEnergyLink Energy => energy;

        private HediffCompProperties_TechnoBit Props => (HediffCompProperties_TechnoBit)props;

        private static readonly HashSet<string> WorkJobs = new HashSet<string> { "DoBill", "FinishFrame", "Repair", "BuildRoof", "Deconstruct", "SmoothFloor", "SmoothWall" };

        public override bool CompShouldRemove => Pawn.Downed || base.CompShouldRemove;

        public Vector3 OrbPos
        {
            get
            {
                var bob = Mathf.Sin(Find.TickManager.TicksGame * 0.05f + Pawn.thingIDNumber) * 0.08f;
                return Pawn.DrawPos + new Vector3(0.7f, 0f, 0.6f + bob);
            }
        }

        public override void DrawAt(Vector3 drawPos)
        {
            if (Graphic is not { } graphic)
                return;

            var pos = OrbPos;
            pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            var size = graphic.drawSize.x;
            var mat = graphic.MatSingle;
            // The glow shader is additive: two layers and a tighter core make the orb read as solid rather than a faint haze.
            DrawLayer(pos, size, mat);
            DrawLayer(pos, size * 0.85f, mat);
            DrawLayer(pos, size * 0.45f, mat);
        }

        private static void DrawLayer(Vector3 pos, float size, Material mat) =>
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(size, 1f, size)), mat, 0);

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            base.CompPostTickInterval(ref severityAdjustment, delta);
            if (!Pawn.Spawned)
                return;

            if (Pawn.Drafted)
            {
                if (Pawn.IsHashIntervalTick(Props.laserInterval, delta))
                    TryShoot(Pawn.IsHashIntervalTick(Props.heavyLaserInterval, delta) ? Props.heavyLaser : Props.laser);
            }
            else if (Props.workBoost != null && Pawn.IsHashIntervalTick(Props.workBoostInterval, delta))
                TryBoostWork();

            if (repairEnabled && Pawn.IsHashIntervalTick(Pawn.Drafted ? Props.repairIntervalDrafted : Props.repairIntervalUndrafted, delta))
                TryRepair();
        }

        private void TryShoot(ThingDef projectileDef)
        {
            if (projectileDef == null || Pawn.stances?.curStance is not Stance_Busy { focusTarg: { IsValid: true } focus })
                return;
            if (focus.Thing is not { Spawned: true } targetThing || !targetThing.HostileTo(Pawn) || !GenSight.LineOfSight(Pawn.Position, targetThing.Position, Pawn.Map))
                return;
            if (!PayForAction(Props.minEnergyToFight))
                return;

            var projectile = (Projectile)GenSpawn.Spawn(projectileDef, Pawn.Position, Pawn.Map);
            projectile.Launch(Pawn, OrbPos, targetThing, targetThing, ProjectileHitFlags.IntendedTarget);
        }

        private void TryRepair()
        {
            var map = Pawn.Map;
            var radiusSq = Props.repairRadius * Props.repairRadius;

            Thing best = null;
            foreach (var building in map.listerBuildingsRepairable.RepairableBuildings(Pawn.Faction))
            {
                if (building.HitPoints < building.MaxHitPoints && (building.Position - Pawn.Position).LengthHorizontalSquared <= radiusSq)
                {
                    best = building;
                    break;
                }
            }

            if (best == null)
            {
                foreach (var pawn in map.mapPawns.SpawnedPawnsInFaction(Pawn.Faction))
                {
                    if (pawn.RaceProps.IsMechanoid && (pawn.Position - Pawn.Position).LengthHorizontalSquared <= radiusSq && AbilityUtility.HasHealableInjury(pawn))
                    {
                        best = pawn;
                        break;
                    }
                }
            }

            if (best == null || !PayForAction(Props.minEnergyToFight))
                return;

            var amount = Props.repairAmount.RandomInRange;
            if (best is Pawn mech)
                AbilityUtility.HealInjuries(mech, 1, amount);
            else
            {
                best.HitPoints = Mathf.Min(best.MaxHitPoints, best.HitPoints + amount);
                map.listerBuildingsRepairable.Notify_BuildingRepaired((Building)best);
            }

            AbilityUtility.ArcBetween(OrbPos, best.DrawPos, map);
        }

        private void TryBoostWork()
        {
            var job = Pawn.CurJob;
            if (job == null || !WorkJobs.Contains(job.def.defName) || !job.targetA.IsValid)
                return;
            if (job.targetA.Cell.DistanceTo(Pawn.Position) > Props.workDistance + 0.5f)
                return;
            if (!PayForAction(Props.minEnergyToAssist))
                return;

            Pawn.health.AddHediff(Props.workBoost);
            FleckMaker.ThrowMicroSparks(OrbPos, Pawn.Map);
        }

        /// <summary>No pool: free. With a pool: needs the given fraction left and pays one action.</summary>
        private bool PayForAction(float minFraction)
        {
            if (energy.Pool(Pawn) is not { } pool)
                return true;
            if (pool.EnergyFraction < minFraction || !pool.CanPay(Props.energyPerAction))
                return false;
            pool.Spend(Props.energyPerAction);
            return true;
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (!Pawn.IsColonistPlayerControlled)
                yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "ABT_TechnoBitRepair".Translate(),
                defaultDesc = "ABT_TechnoBitRepairDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/Install"),
                isActive = () => repairEnabled,
                toggleAction = () => repairEnabled = !repairEnabled
            };
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref repairEnabled, "repairEnabled", true);
            Scribe_Deep.Look(ref energy, "energy");
            energy ??= new AbilityEnergyLink();
        }
    }
}
