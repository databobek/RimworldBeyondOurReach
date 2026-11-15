using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VEF.Abilities;
using Verse;
using Ability = VEF.Abilities.Ability;

using AllBambaArmors;
using AbilityUtility = AllBambaArmors.AbilityUtility;

namespace BeyondOurReach
{
    /// <summary>Disables the ability while the caster has a given hediff (e.g. psionic exhaustion).</summary>
    public class AbilityExtension_BlockedByHediff : AbilityExtension_AbilityMod
    {
        public HediffDef hediff;

        public override bool IsEnabledForPawn(Ability ability, out string reason)
        {
            if (ability.pawn.health.hediffSet.GetFirstHediffOfDef(hediff) is { } blocking)
            {
                reason = "ABT_BlockedByHediff".Translate(blocking.LabelCap);
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }

    /// <summary>Heals several injuries on the target pawn at once, optionally stopping missing-part bleeding.</summary>
    public class AbilityExtension_HealInjuries : AbilityExtension_AbilityMod
    {
        public int injuries = 3;
        public float amount = 10f;
        public bool stopMissingPartBleeding;

        public override bool ValidateTarget(LocalTargetInfo target, Ability ability, bool throwMessages = false)
        {
            if (target.Pawn is { } pawn && !AbilityUtility.HasHealableInjury(pawn) && !(stopMissingPartBleeding && AbilityUtility.HasBleedingMissingPart(pawn)))
            {
                if (throwMessages)
                    Messages.Message("ABT_NothingToHeal".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.ValidateTarget(target, ability, throwMessages);
        }

        public override void Cast(GlobalTargetInfo[] targets, Ability ability)
        {
            base.Cast(targets, ability);
            foreach (var target in targets)
            {
                if (target.Thing is not Pawn pawn)
                    continue;
                if (stopMissingPartBleeding)
                    AbilityUtility.StopMissingPartBleeding(pawn);
                AbilityUtility.HealInjuries(pawn, injuries, amount);
            }
        }

        public override string GetDescription(Ability ability) =>
            "ABT_HealDesc".Translate(injuries, amount.ToString("0")).Colorize(UnityEngine.Color.cyan);
    }

    /// <summary>
    /// Removes or reduces conditions on the target. Entries are checked in order and at most <see cref="maxConditions"/> are fixed per cast.
    /// Used by Cure Disease (first listed disease found, 75% success) and Purify (scars, then chronic illness, then addictions).
    /// </summary>
    public class AbilityExtension_Cure : AbilityExtension_AbilityMod
    {
        public float successChance = 1f;
        public int maxConditions = 1;

        /// <summary>HP healed on each permanent injury (scar). 0 skips scars.</summary>
        public float scarHealing;
        public bool scarsIncludeBrain;

        public List<CureEntry> conditions = new List<CureEntry>();

        public class CureEntry
        {
            public HediffDef hediff;
            /// <summary>Severity removed per cast. 0 or less removes the hediff outright.</summary>
            public float severity;
            /// <summary>Chance the fix works this cast. A failed roll still uses up the cast.</summary>
            public float chance = 1f;
        }

        public override bool ValidateTarget(LocalTargetInfo target, Ability ability, bool throwMessages = false)
        {
            if (target.Pawn is { } pawn && !FindFixes(pawn).Any())
            {
                if (throwMessages)
                    Messages.Message("ABT_NothingToCure".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return base.ValidateTarget(target, ability, throwMessages);
        }

        public override void Cast(GlobalTargetInfo[] targets, Ability ability)
        {
            base.Cast(targets, ability);
            foreach (var target in targets)
            {
                if (target.Thing is not Pawn pawn)
                    continue;

                if (!Rand.Chance(successChance))
                {
                    Messages.Message("ABT_CureFailed".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.NegativeEvent, false);
                    continue;
                }

                foreach (var fix in FindFixes(pawn).Take(maxConditions).ToList())
                    fix();

                if (pawn.Spawned)
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.HealingCross);
            }
        }

        /// <summary>One action per fixable condition, in priority order.</summary>
        private IEnumerable<System.Action> FindFixes(Pawn pawn)
        {
            var hediffs = pawn.health.hediffSet.hediffs;

            if (scarHealing > 0f)
            {
                foreach (var hediff in hediffs)
                {
                    if (hediff is Hediff_Injury injury && injury.IsPermanent() && (scarsIncludeBrain || injury.Part != pawn.health.hediffSet.GetBrain()))
                    {
                        var scar = injury;
                        yield return () => scar.Heal(scarHealing);
                    }
                }
            }

            foreach (var entry in conditions)
            {
                foreach (var hediff in hediffs)
                {
                    if (hediff.def != entry.hediff)
                        continue;

                    var found = hediff;
                    var reduction = entry.severity;
                    var chance = entry.chance;
                    yield return () =>
                    {
                        if (!Rand.Chance(chance))
                            return;
                        if (reduction <= 0f || found.Severity - reduction <= 0.001f)
                            pawn.health.RemoveHediff(found);
                        else
                            found.Severity -= reduction;
                    };
                }
            }
        }
    }

    /// <summary>Spawns an <see cref="AbilityField"/> (or any thing) at the target cell, linked to the caster.</summary>
    public class AbilityExtension_SpawnField : AbilityExtension_AbilityMod
    {
        public ThingDef thingDef;
        public bool onCaster;

        public override void Cast(GlobalTargetInfo[] targets, Ability ability)
        {
            base.Cast(targets, ability);
            var pawn = ability.pawn;
            var map = pawn.MapHeld;
            if (map == null || targets.Length == 0)
                return;

            var cell = onCaster ? pawn.PositionHeld : targets[0].Cell;
            var thing = ThingMaker.MakeThing(thingDef);
            if (thing is AbilityField field)
                field.Setup(pawn, targets[0].HasThing ? new LocalTargetInfo(targets[0].Thing) : new LocalTargetInfo(cell), ability.GetRadiusForPawn(), ability.GetPowerForPawn());
            GenSpawn.Spawn(thing, cell, map);
        }
    }
}
