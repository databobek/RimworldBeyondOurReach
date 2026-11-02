using System.Reflection;
using AM;
using UnityEngine;
using Verse;

namespace BeyondOurReach.MeleeAnimation;

// Melee Animation draws weapons itself, so the held-weapon glow never runs during its animations. Named by
// CustomRendererClass in each weapon's tweak data, this adds the glow with the placement the animation worked out.
public class WeaponGlowPartRenderer : PartRenderer
{
    // Melee Animation is built against Unity's own assemblies, which the game maps onto its engine at runtime. The compiler
    // can't see that, so these two fields have to be read by reflection.
    private static readonly FieldInfo MeshField = typeof(PartRenderer).GetField("Mesh")!;
    private static readonly FieldInfo TrsField = typeof(PartRenderer).GetField("TRS")!;

    private static bool failed;

    public override bool Draw()
    {
        if (!failed)
        {
            try
            {
                CompWeaponGlow? glow = OverrideData.Weapon?.TryGetComp<CompWeaponGlow>();
                glow?.Draw((Mesh)MeshField.GetValue(this)!, (Matrix4x4)TrsField.GetValue(this)!);
            }
            catch (System.Exception e)
            {
                failed = true;
                Log.Warning("[BOR] Weapon glow is off during Melee Animation: " + e.Message);
            }
        }
        // Not handled, so the mod still draws the weapon itself.
        return false;
    }
}
