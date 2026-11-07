using System.Linq;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Implemented by a comp that knows when its building is in use, so the glow shows only then.
public interface IGlowSource
{
    bool GlowActive { get; }
}

// Draws a glow texture over the building while any comp of it that is an IGlowSource is active. The texture has the building's own size and shows only the parts that glow,
// so it lines up with whatever drawSize and drawOffset the graphic has. The building needs drawerType MapMeshAndRealTime.
public class CompProperties_GlowOverlay : CompProperties
{
    public string glowTexPath = "";
    public int pulseTicks = 240;
    public float pulseStrength = 0.7f;
    // Each pass adds the glow texture's light on top once more, for a brighter glow.
    public int passes = 1;

    public CompProperties_GlowOverlay()
    {
        compClass = typeof(CompGlowOverlay);
    }
}

public sealed class CompGlowOverlay : ThingComp
{
    // Above the 0.01 slope of the building texture, and in one queue for the same reason as the mine's glow.
    private const float GlowHeight = 0.012f;
    private const int RenderQueue = 2960;

    private Material? glowMaterial;
    private IGlowSource? source;

    private CompProperties_GlowOverlay Props => (CompProperties_GlowOverlay)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        source = parent.AllComps.OfType<IGlowSource>().FirstOrDefault();
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (source is not { GlowActive: true })
        {
            return;
        }
        glowMaterial ??= MaterialPool.MatFrom(Props.glowTexPath, ShaderDatabase.MoteGlow);

        float pulse = 0.5f + 0.5f * Mathf.Sin(Find.TickManager.TicksGame * 2f * Mathf.PI / Props.pulseTicks);
        Material faded = FadedMaterialPool.FadedVersionOf(glowMaterial, 1f - Props.pulseStrength * (1f - pulse));

        Vector3 center = parent.DrawPos + parent.Graphic.DrawOffset(parent.Rotation);
        Vector2 size = parent.Graphic.drawSize;
        Matrix4x4 matrix = Matrix4x4.TRS(center + new Vector3(0f, GlowHeight, 0f), Quaternion.identity, new Vector3(size.x, 1f, size.y));
        for (int i = 0; i < Props.passes; i++)
        {
            Graphics.DrawMesh(MeshPool.plane10, matrix, faded, 0, null!, 0, (MaterialPropertyBlock)null!, UnityEngine.Rendering.ShadowCastingMode.Off, false, RenderQueue);
        }
    }
}
