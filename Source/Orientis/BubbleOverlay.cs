using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Experiment: draws transparent bubble layers over a building and pops each one in and out, growing and shrinking it around its pivot. Every layer texture has the building's
// own size, and the bubbles are cut off wherever the mask texture is not red. The building needs drawerType MapMeshAndRealTime.
// There is no shader for masking, so all layers are blended on the CPU into one texture that every building of the def shares.
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
    // The shared texture is rebuilt this often, in ticks.
    public int updateTicks = 2;

    internal BubbleComposite? composite;

    public CompProperties_BubbleOverlay()
    {
        compClass = typeof(CompBubbleOverlay);
    }
}

// The blended bubbles of one def. Pixel (0,0) is the bottom left, as in every Unity texture, so layers, mask and result line up without flipping.
internal sealed class BubbleComposite
{
    private sealed class Layer
    {
        public Color32[] Pixels = Array.Empty<Color32>();
        public int MinX, MaxX, MinY, MaxY;
        public Vector2 Pivot;
    }

    private readonly CompProperties_BubbleOverlay props;
    private readonly int width;
    private readonly int height;
    private readonly bool[] mask;
    private readonly int maskMinX, maskMaxX, maskMinY, maskMaxY;
    private readonly List<Layer> layers = new();
    private readonly Color32[] output;
    private int lastStep = int.MinValue;

    public Texture2D Texture { get; }
    public Material Material { get; }

    public BubbleComposite(CompProperties_BubbleOverlay props, ModContentPack pack)
    {
        this.props = props;
        Color32[] maskPixels = Load(pack, props.maskTexPath, out width, out height);
        mask = new bool[maskPixels.Length];
        maskMinX = maskMinY = int.MaxValue;
        maskMaxX = maskMaxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (maskPixels[y * width + x].r > 127)
                {
                    mask[y * width + x] = true;
                    maskMinX = Mathf.Min(maskMinX, x);
                    maskMaxX = Mathf.Max(maskMaxX, x);
                    maskMinY = Mathf.Min(maskMinY, y);
                    maskMaxY = Mathf.Max(maskMaxY, y);
                }
            }
        }

        for (int i = 0; i < props.layerTexPaths.Count; i++)
        {
            Layer layer = new()
            {
                Pixels = Load(pack, props.layerTexPaths[i], out int w, out int h),
                MinX = int.MaxValue,
                MinY = int.MaxValue,
                MaxX = -1,
                MaxY = -1
            };
            if (w != width || h != height)
            {
                Log.Error($"[BeyondOurReach] Bubble layer {props.layerTexPaths[i]} is {w}x{h}, but the mask is {width}x{height}.");
                layer.Pixels = new Color32[width * height];
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (layer.Pixels[y * width + x].a > 0)
                    {
                        layer.MinX = Mathf.Min(layer.MinX, x);
                        layer.MaxX = Mathf.Max(layer.MaxX, x);
                        layer.MinY = Mathf.Min(layer.MinY, y);
                        layer.MaxY = Mathf.Max(layer.MaxY, y);
                    }
                }
            }
            Vector2 pivot = i < props.pivots.Count ? props.pivots[i] : new Vector2(0.5f, 0.5f);
            layer.Pivot = new Vector2(pivot.x * width, pivot.y * height);
            layers.Add(layer);
        }

        output = new Color32[width * height];
        Texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        Material = new Material(ShaderDatabase.Transparent) { mainTexture = Texture };
    }

    private static Color32[] Load(ModContentPack pack, string texPath, out int width, out int height)
    {
        string file = Path.Combine(pack.RootDir, "Textures", texPath + ".png");
        Texture2D tex = new(2, 2, TextureFormat.RGBA32, false);
        if (!File.Exists(file) || !tex.LoadImage(File.ReadAllBytes(file)))
        {
            Log.Error($"[BeyondOurReach] Could not load bubble texture {file}.");
        }
        width = tex.width;
        height = tex.height;
        Color32[] pixels = tex.GetPixels32();
        UnityEngine.Object.Destroy(tex);
        return pixels;
    }

    // Rebuilds the texture when the animation has moved on to a new step.
    public void Update(int ticks)
    {
        int step = ticks / Mathf.Max(1, props.updateTicks);
        if (step == lastStep)
        {
            return;
        }
        lastStep = step;

        Array.Clear(output, 0, output.Length);
        for (int i = 0; i < layers.Count; i++)
        {
            float t = Mathf.Repeat((float)ticks / props.cycleTicks + (float)i / layers.Count, 1f);
            float visible = 1f - props.hiddenFraction;
            if (t >= visible)
            {
                continue;
            }
            float life = t / visible;
            // Rises fast, hangs near full size with a slow wobble, then collapses.
            float envelope = Mathf.Sqrt(Mathf.Sin(life * Mathf.PI));
            float wobble = 1f + 0.06f * Mathf.Sin(life * Mathf.PI * 6f);
            float scale = envelope * wobble * props.maxScale;
            if (scale < 0.02f)
            {
                continue;
            }
            Blend(layers[i], scale, Mathf.Clamp01(envelope * 1.5f));
        }
        Texture.SetPixels32(output);
        Texture.Apply(false);
    }

    private void Blend(Layer layer, float scale, float opacity)
    {
        if (layer.MaxX < 0)
        {
            return;
        }
        Vector2 pivot = layer.Pivot;
        int x0 = Mathf.Max(maskMinX, Mathf.FloorToInt(pivot.x + (layer.MinX - pivot.x) * scale) - 1);
        int x1 = Mathf.Min(maskMaxX, Mathf.CeilToInt(pivot.x + (layer.MaxX + 1 - pivot.x) * scale) + 1);
        int y0 = Mathf.Max(maskMinY, Mathf.FloorToInt(pivot.y + (layer.MinY - pivot.y) * scale) - 1);
        int y1 = Mathf.Min(maskMaxY, Mathf.CeilToInt(pivot.y + (layer.MaxY + 1 - pivot.y) * scale) + 1);

        for (int y = y0; y <= y1; y++)
        {
            float sy = pivot.y + (y + 0.5f - pivot.y) / scale - 0.5f;
            for (int x = x0; x <= x1; x++)
            {
                if (!mask[y * width + x])
                {
                    continue;
                }
                float sx = pivot.x + (x + 0.5f - pivot.x) / scale - 0.5f;
                Sample(layer.Pixels, sx, sy, out float r, out float g, out float b, out float a);
                a *= opacity;
                if (a <= 0f)
                {
                    continue;
                }
                Color32 under = output[y * width + x];
                float ua = under.a / 255f * (1f - a);
                float oa = a + ua;
                output[y * width + x] = new Color32(
                    (byte)((r * a + under.r * ua) / oa),
                    (byte)((g * a + under.g * ua) / oa),
                    (byte)((b * a + under.b * ua) / oa),
                    (byte)(oa * 255f));
            }
        }
    }

    // Bilinear sample with color weighted by alpha, so transparent pixels do not tint the edges. Out of range counts as transparent.
    private void Sample(Color32[] pixels, float sx, float sy, out float r, out float g, out float b, out float a)
    {
        int ix = Mathf.FloorToInt(sx);
        int iy = Mathf.FloorToInt(sy);
        float fx = sx - ix;
        float fy = sy - iy;
        float sr = 0f, sg = 0f, sb = 0f, sa = 0f;
        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 2; i++)
            {
                int px = ix + i;
                int py = iy + j;
                if (px < 0 || py < 0 || px >= width || py >= height)
                {
                    continue;
                }
                float w = (i == 0 ? 1f - fx : fx) * (j == 0 ? 1f - fy : fy);
                Color32 c = pixels[py * width + px];
                float pa = c.a / 255f * w;
                sr += c.r * pa;
                sg += c.g * pa;
                sb += c.b * pa;
                sa += pa;
            }
        }
        a = sa;
        if (sa <= 0f)
        {
            r = g = b = 0f;
            return;
        }
        r = sr / sa;
        g = sg / sa;
        b = sb / sa;
    }
}

public sealed class CompBubbleOverlay : ThingComp
{
    private const float LayerHeight = 0.012f;
    private const int RenderQueue = 2960;

    private CompProperties_BubbleOverlay Props => (CompProperties_BubbleOverlay)props;

    public override void PostDraw()
    {
        base.PostDraw();
        if (Props.layerTexPaths.Count == 0 || parent.def.modContentPack == null)
        {
            return;
        }
        BubbleComposite composite = Props.composite ??= new BubbleComposite(Props, parent.def.modContentPack);
        composite.Update(Find.TickManager.TicksGame);

        Vector3 center = parent.DrawPos + parent.Graphic.DrawOffset(parent.Rotation);
        Vector2 size = parent.Graphic.drawSize;
        Matrix4x4 matrix = Matrix4x4.TRS(center + new Vector3(0f, LayerHeight, 0f), Quaternion.identity, new Vector3(size.x, 1f, size.y));
        Graphics.DrawMesh(MeshPool.plane10, matrix, composite.Material, 0, null!, 0, (MaterialPropertyBlock)null!, UnityEngine.Rendering.ShadowCastingMode.Off, false, RenderQueue);
    }
}
