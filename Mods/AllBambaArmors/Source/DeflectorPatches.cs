using System.Linq;
using HarmonyLib;
using Verse;
using AllBambaArmors;
using AbilityUtility = AllBambaArmors.AbilityUtility;

namespace BeyondOurReach
{
    /// <summary>
    /// The vanilla interceptor only stops projectiles. Fire spews (scorcher), beams and arc sprays damage cells directly,
    /// and blasts reach in from outside, so these prefixes skip any damaged cell inside an "archotech protects" bubble when the attack
    /// started outside it. Each prefix exits at once when no deflector is up.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class DeflectorPatches
    {
        static DeflectorPatches()
        {
            var harmony = new Harmony("bamba.allbambaarmors.deflector");

            // Explosions, including the scorcher's cone of flame: source is the blast center.
            harmony.Patch(AccessTools.Method(typeof(Explosion), "AffectCell"), prefix: new HarmonyMethod(typeof(DeflectorPatches), nameof(ExplosionPrefix)));

            // Beam weapons: HitCell(cell, sourceCell, damageFactor).
            harmony.Patch(AccessTools.Method(typeof(Verb_ShootBeam), "HitCell"), prefix: new HarmonyMethod(typeof(DeflectorPatches), nameof(BeamPrefix)));

            // Sprays: HitCell(cell) is virtual, so patch the base and every override, modded ones included.
            foreach (var type in typeof(Verb_Spray).AllSubclasses().Prepend(typeof(Verb_Spray)))
            {
                var method = AccessTools.DeclaredMethod(type, "HitCell", new[] { typeof(IntVec3) });
                if (method != null && !method.IsAbstract)
                    harmony.Patch(method, prefix: new HarmonyMethod(typeof(DeflectorPatches), nameof(SprayPrefix)));
            }
        }

        private static bool ExplosionPrefix(Explosion __instance, IntVec3 __0) =>
            !Thing_PsionicBarrier.Blocks(__instance.Map, __instance.Position, __0);

        private static bool BeamPrefix(Verb __instance, IntVec3 __0, IntVec3 __1) =>
            !Thing_PsionicBarrier.Blocks(__instance.caster?.Map, __1, __0);

        private static bool SprayPrefix(Verb __instance, IntVec3 __0) =>
            __instance.caster == null || !Thing_PsionicBarrier.Blocks(__instance.caster.Map, __instance.caster.Position, __0);
    }
}
