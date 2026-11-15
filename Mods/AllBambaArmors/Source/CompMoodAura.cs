using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using AllBambaArmors;
using AbilityUtility = AllBambaArmors.AbilityUtility;

namespace BeyondOurReach
{
    /// <summary>One rule: pawns matching any of the listed markers get <see cref="thought"/>.</summary>
    public class MoodAuraEntry
    {
        public ThoughtDef thought;
        public List<HediffDef> hediffs = new List<HediffDef>();
        public List<XenotypeDef> xenotypes = new List<XenotypeDef>();
        public List<FactionDef> factions = new List<FactionDef>();
        /// <summary>Matches a pawn with any hediff whose def carries this mod extension (e.g. a family of implants).</summary>
        public Type hediffExtension;

        public bool Matches(Pawn pawn)
        {
            if (factions.Count > 0 && (factions.Contains(pawn.Faction?.def) || factions.Contains(pawn.kindDef?.defaultFactionDef)))
                return true;
            if (xenotypes.Count > 0 && pawn.genes?.Xenotype is { } xenotype && xenotypes.Contains(xenotype))
                return true;

            var list = pawn.health.hediffSet.hediffs;
            for (var i = 0; i < list.Count; i++)
            {
                var def = list[i].def;
                if (hediffs.Contains(def))
                    return true;
                if (hediffExtension != null && def.modExtensions != null && def.modExtensions.Exists(e => hediffExtension.IsInstanceOfType(e)))
                    return true;
            }
            return false;
        }
    }

    public class CompProperties_MoodAura : CompProperties
    {
        public float radius = 14f;
        /// <summary>Checked in order; a pawn gets the thought of the first rule it matches.</summary>
        public List<MoodAuraEntry> entries = new List<MoodAuraEntry>();

        public CompProperties_MoodAura()
        {
            compClass = typeof(CompMoodAura);
        }
    }

    /// <summary>
    /// Every rare tick, gives humanlikes within the radius the thought of the first rule they match. The thoughts are short memories,
    /// so standing near keeps them topped up and they fade soon after leaving. Without rules (no BOR) it does nothing.
    /// </summary>
    public class CompMoodAura : ThingComp
    {
        private CompProperties_MoodAura Props => (CompProperties_MoodAura)props;

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (!parent.Spawned || Props.entries.Count == 0)
                return;

            var radiusSq = Props.radius * Props.radius;
            var pawns = parent.Map.mapPawns.AllHumanlikeSpawned;
            for (var i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                if (pawn.Dead || pawn.needs?.mood == null || (pawn.Position - parent.Position).LengthHorizontalSquared > radiusSq)
                    continue;

                foreach (var entry in Props.entries)
                {
                    if (entry.thought == null || !entry.Matches(pawn))
                        continue;
                    pawn.needs.mood.thoughts.memories.TryGainMemory(entry.thought);
                    break;
                }
            }
        }
    }
}
