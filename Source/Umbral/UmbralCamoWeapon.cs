using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Vanilla draws the held weapon outside the pawn's render tree, so the invisibility material the tree uses for the body
// never reaches it and a camouflaged Umbral's weapon floats in plain sight. While the equipment of a pawn in Umbral camo
// is being drawn, the weapon gets the same invisible material. Other invisible pawns keep vanilla behaviour.
// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class UmbralCamoWeapon
{
    [System.ThreadStatic]
    private static bool drawingCamoPawn;

    private static bool InCamo(Pawn pawn, PawnRenderFlags flags) =>
        !flags.FlagSet(PawnRenderFlags.Portrait) && !flags.FlagSet(PawnRenderFlags.Cache)
        && pawn.IsPsychologicallyInvisible() && pawn.health.hediffSet.HasHediff(BOR_UmbralDefOf.BOR_UmbralCamo);

    private static Material CamoMaterial(Material material) =>
        drawingCamoPawn ? InvisibilityMatPool.GetInvisibleMat(material) : material;

    // Stands in for the Graphics.DrawMesh call in DrawEquipmentAiming.
    public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer) =>
        Graphics.DrawMesh(mesh, matrix, CamoMaterial(material), layer);

    // Combat Extended's transpiler swaps the same draw call for its own DrawMesh, which draws the gun with recoil.
    // Whichever transpiler runs second, CE's method ends up drawing the gun, so its material is swapped here instead.
    private static readonly MethodInfo? CeDrawMesh = ModsConfig.IsActive("ceteam.combatextended")
        ? AccessTools.Method(AccessTools.TypeByName("CombatExtended.HarmonyCE.Harmony_PawnRenderer_DrawEquipmentAiming"), "DrawMesh")
        : null;

    private static bool warned;

    [HarmonyPatch]
    public static class CeDrawMesh_Patch
    {
        public static bool Prepare() => CeDrawMesh != null;

        public static MethodBase TargetMethod() => CeDrawMesh!;

        // The material is CE's third parameter (mesh, matrix, mat, layer, ...).
        public static void Prefix(ref Material __2) => __2 = CamoMaterial(__2);
    }

    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    public static class DrawEquipmentAndApparelExtras_Patch
    {
        public static void Prefix(Pawn pawn, PawnRenderFlags flags) => drawingCamoPawn = InCamo(pawn, flags);

        public static void Finalizer() => drawingCamoPawn = false;
    }

    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
    public static class DrawEquipmentAiming_Patch
    {
        private static readonly MethodInfo Vanilla = AccessTools.Method(typeof(Graphics), nameof(Graphics.DrawMesh),
            new[] { typeof(Mesh), typeof(Matrix4x4), typeof(Material), typeof(int) });

        private static readonly MethodInfo Replacement = AccessTools.Method(typeof(UmbralCamoWeapon), nameof(DrawMesh));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool handled = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(Vanilla))
                {
                    instruction.operand = Replacement;
                    handled = true;
                }
                else if (CeDrawMesh != null && instruction.Calls(CeDrawMesh))
                {
                    handled = true;
                }
                yield return instruction;
            }
            // Harmony reruns every transpiler each time another mod patches this method, so warn only once.
            if (!handled && !warned)
            {
                warned = true;
                Log.Warning("[BOR] Umbral camo: weapon draw call not found, weapons stay visible in camo.");
            }
        }
    }
}
