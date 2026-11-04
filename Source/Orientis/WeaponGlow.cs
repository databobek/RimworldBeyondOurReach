using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class CompProperties_WeaponGlow : CompProperties
{
    // Same size as the weapon's texture, with only the part that glows painted in.
    public string texPath = "";

    public float strength = 0.6f;

    public CompProperties_WeaponGlow()
    {
        compClass = typeof(CompWeaponGlow);
    }
}

public sealed class CompWeaponGlow : ThingComp
{
    private Material? material;

    public CompProperties_WeaponGlow Props => (CompProperties_WeaponGlow)props;

    public void Draw(Mesh mesh, Matrix4x4 matrix)
    {
        material ??= MaterialPool.MatFrom(Props.texPath, ShaderDatabase.MoteGlow);
        Graphics.DrawMesh(mesh, matrix, FadedMaterialPool.FadedVersionOf(material, Props.strength), 0);
    }
}

// Draws the glow over a held weapon. Vanilla's method keeps its placement to itself, so this works it out the same way.
[HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
public static class PawnRenderUtility_DrawEquipmentAiming_Patch
{
    private const float GlowHeight = 0.001f;

    // Melee Animation skips the original while it draws the weapon itself, and postfixes run regardless.
    public static void Postfix(Thing eq, Vector3 drawLoc, float aimAngle, bool __runOriginal)
    {
        CompWeaponGlow? glow = __runOriginal ? eq.TryGetComp<CompWeaponGlow>() : null;
        if (glow == null)
        {
            return;
        }
        float angle = aimAngle - 90f;
        Mesh mesh = MeshPool.plane10;
        if (aimAngle > 200f && aimAngle < 340f)
        {
            mesh = MeshPool.plane10Flip;
            angle -= 180f + eq.def.equippedAngleOffset;
        }
        else
        {
            angle += eq.def.equippedAngleOffset;
        }
        angle %= 360f;
        CompEquippable? equippable = eq.TryGetComp<CompEquippable>();
        if (equippable != null)
        {
            EquipmentUtility.Recoil(eq.def, EquipmentUtility.GetRecoilVerb(equippable.AllVerbs), out Vector3 drawOffset, out float angleOffset, aimAngle);
            drawLoc += drawOffset;
            angle += angleOffset;
        }
        Vector3 size = new(eq.Graphic.drawSize.x, 0f, eq.Graphic.drawSize.y);
        glow.Draw(mesh, Matrix4x4.TRS(drawLoc + Vector3.up * GlowHeight, Quaternion.AngleAxis(angle, Vector3.up), size));
    }
}
