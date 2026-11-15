using RimWorld;
using Verse;

namespace BeyondOurReach
{
    /// <summary>Defs from Defs/ in this folder, used by the abilities in this assembly.</summary>
    [DefOf]
    public static class BOR_ArcaneDefOf
    {
        public static HediffDef ABT_BestowMightHediff;
        public static HediffDef ABT_HolyWardHediff;
        public static HediffDef ABT_ResurrectedHediff;
        public static HediffDef ABT_ResurrectionDrainHediff;
        public static HediffDef ABT_PsionicExhaustionHediff;
        public static ThingDef ABT_SunfireBeam;
        public static ThingDef ABT_PsionicBarrierField;
        public static ThingDef ABT_LightOrb;
        public static JobDef ABT_ChannelBarrier;
        public static DamageDef ABT_Lightning;
        public static DamageDef ABT_VaporizeBlast;
        public static FleckDef ABT_GoldenMist;

        static BOR_ArcaneDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BOR_ArcaneDefOf));
    }
}
