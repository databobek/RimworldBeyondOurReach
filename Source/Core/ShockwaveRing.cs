using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// A flat ring racing out across a map: the Path of Light's golden shockwave and the dark mirror's red pulse.
public static class ShockwaveRing
{
    // From the center to the farthest corner of the map.
    public static float MaxRadius(Map map, IntVec3 center) =>
        Mathf.Sqrt(Square(Mathf.Max(center.x, map.Size.x - center.x)) + Square(Mathf.Max(center.z, map.Size.z - center.z)));

    private static float Square(float value) => value * value;

    // Fades to 30% as it reaches the edge. Only drawn on the map being looked at.
    public static void Draw(Map map, IntVec3 center, float radius, Material material)
    {
        if (map != Find.CurrentMap || map == null || WorldRendererUtility.WorldRendered)
        {
            return;
        }
        float alpha = 1f - Mathf.Clamp01(radius / MaxRadius(map, center)) * 0.7f;
        Vector3 pos = center.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather);
        Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(radius * 2f, 1f, radius * 2f));
        Graphics.DrawMesh(MeshPool.plane10, matrix, Faded(material, alpha), 0);
    }

    private const int FadeSteps = 30;

    private static readonly Dictionary<(Material, int), Material> faded = new();

    // Like FadedMaterialPool, but keeps the material's tint: vanilla resets faded copies to white, which turned the red
    // pulse into the texture's gold as soon as it began to fade.
    private static Material Faded(Material source, float alpha)
    {
        int step = Mathf.Clamp(Mathf.FloorToInt(alpha * FadeSteps), 0, FadeSteps - 1);
        if (step == FadeSteps - 1)
        {
            return source;
        }
        if (!faded.TryGetValue((source, step), out Material? mat))
        {
            mat = new Material(source);
            Color tint = source.color;
            mat.color = new Color(tint.r, tint.g, tint.b, tint.a * step / FadeSteps);
            faded[(source, step)] = mat;
        }
        return mat;
    }
}
