using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Experiment: draws transparent bubble layers over a building and pops each one in and out, growing and shrinking it around its pivot. Every layer texture has the building's
// own size, and the bubbles are cut off wherever the mask texture is not red. The building needs drawerType MapMeshAndRealTime.
// The scaling and masking happen in the BOR_BubbleMask shader (Common/Shaders), one draw per layer.
public class CompProperties_BubbleOverlay : CompProperties
{
    public List<string> layerTexPaths = new();
    // The mask has the same size as the layers; bubbles show only where its red channel is set.
    public string maskTexPath = "";
    // Where each layer scales from, as a fraction of the texture (0,0 is bottom left). Missing entries use the center.
    public List<Vector2> pivots = new();
    public int cycleTicks = 300;
    // Share of the cycle the layer spends gone. The rest is pop in, wobble, pop out.
    public float hiddenFraction = 0.25f;
    public float maxScale = 1.15f;

    // One per layer, shared by every building of the def: the animation runs on game ticks, so they all look the same.
    internal List<Material>? materials;

    public CompProperties_BubbleOverlay()
    {
        compClass = typeof(CompBubbleOverlay);
    }

    internal List<Material> Materials()
    {
        if (materials != null)
        {
            return materials;
        }
        materials = new List<Material>();
        Shader shader = DefDatabase<ShaderDef>.GetNamed("BOR_BubbleMask").Shader;
        Texture2D mask = ContentFinder<Texture2D>.Get(maskTexPath);
        foreach (string path in layerTexPaths)
        {
            Material material = new(shader) { mainTexture = ContentFinder<Texture2D>.Get(path) };
            material.SetTexture("_MaskTex", mask);
            materials.Add(material);
        }
        return materials;
    }
}

public sealed class CompBubbleOverlay : ThingComp
{
    private const float LayerHeight = 0.012f;
    // Each layer draws over the ones before it.
    private const int RenderQueue = 2960;

    private CompProperties_BubbleOverlay Props => (CompProperties_BubbleOverlay)props;

    public override void PostDraw()
    {
        base.PostDraw();
        if (Props.layerTexPaths.Count == 0)
        {
            return;
        }
        List<Material> materials = Props.Materials();
        Vector3 center = parent.DrawPos + parent.Graphic.DrawOffset(parent.Rotation);
        Vector2 size = parent.Graphic.drawSize;
        Matrix4x4 matrix = Matrix4x4.TRS(center + new Vector3(0f, LayerHeight, 0f), Quaternion.identity, new Vector3(size.x, 1f, size.y));
        int ticks = Find.TickManager.TicksGame;
        for (int i = 0; i < materials.Count; i++)
        {
            float t = Mathf.Repeat((float)ticks / Props.cycleTicks + (float)i / materials.Count, 1f);
            float visible = 1f - Props.hiddenFraction;
            if (t >= visible)
            {
                continue;
            }
            float life = t / visible;
            // Rises fast, hangs near full size with a slow wobble, then collapses.
            float envelope = Mathf.Sqrt(Mathf.Sin(life * Mathf.PI));
            float wobble = 1f + 0.06f * Mathf.Sin(life * Mathf.PI * 6f);
            float scale = envelope * wobble * Props.maxScale;
            if (scale < 0.02f)
            {
                continue;
            }
            Vector2 pivot = i < Props.pivots.Count ? Props.pivots[i] : new Vector2(0.5f, 0.5f);
            materials[i].SetVector("_Bubble", new Vector4(pivot.x, pivot.y, 1f / scale, Mathf.Clamp01(envelope * 1.5f)));
            Graphics.DrawMesh(MeshPool.plane10, matrix, materials[i], 0, null!, 0, (MaterialPropertyBlock)null!,
                UnityEngine.Rendering.ShadowCastingMode.Off, false, RenderQueue + i);
        }
    }
}
