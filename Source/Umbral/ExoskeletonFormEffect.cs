using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// While an exoskeleton switches form the pawn wears the no-form suit and helmet, and pictures of the form's armor and
// helmet are drawn over them, with a glowing seam along the cut edge: first the old form sheds from the bottom up, then
// the new one grows on from the top down. The real pieces are only worn once the switch ends. Drawn every frame like a mote.
public sealed class MapComponent_ExoskeletonForms : MapComponent
{
    // Render tree layers (PawnRenderTreeDefs): body apparel from 20, head 50, headgear from 70. The armor goes over the body
    // apparel but under the head, so the collar stays behind it; the helmet goes over the worn headgear. Each seam goes a
    // fraction of a layer above its piece.
    private static readonly float ArmorHeight = PawnRenderUtility.AltitudeForLayer(29f);
    private static readonly float HelmetHeight = PawnRenderUtility.AltitudeForLayer(85f);
    private static readonly float SeamLift = PawnRenderUtility.AltitudeForLayer(0.5f);

    private static readonly Color SeamColor = new(1f, 0.2f, 0.25f, 1f);

    private readonly HashSet<HediffComp_UmbralModeApparel> forming = new();

    // One unspawned piece per apparel def, only used to look up its worn graphic.
    private static readonly Dictionary<ThingDef, Apparel> samples = new();

    public MapComponent_ExoskeletonForms(Map map) : base(map)
    {
    }

    public static MapComponent_ExoskeletonForms Of(Map map) => map.GetComponent<MapComponent_ExoskeletonForms>();

    public void Add(HediffComp_UmbralModeApparel comp) => forming.Add(comp);

    public override void MapComponentUpdate()
    {
        if (forming.Count == 0)
        {
            return;
        }
        forming.RemoveWhere(c => !c.Switching || c.Pawn.Map != map || !c.Pawn.Spawned);
        foreach (HediffComp_UmbralModeApparel comp in forming)
        {
            Pawn pawn = comp.Pawn;
            if (!comp.TryGetSwitchFrame(out int form, out float shown) || pawn.story?.bodyType == null
                || pawn.GetPosture() != PawnPosture.Standing || pawn.Map.fogGrid.IsFogged(pawn.Position))
            {
                continue;
            }
            Rot4 rot = pawn.Rotation;
            Vector3 root = pawn.DrawPos;
            if (comp.ApparelFor(form) is { } armor)
            {
                Vector3 size = HumanlikeMeshPoolUtility.GetHumanlikeBodySetForPawn(pawn).MeshAt(rot).bounds.size;
                DrawPiece(pawn, armor, rot, root + new Vector3(0f, ArmorHeight, 0f), size, shown);
            }
            if (comp.SkullComp?.ApparelFor(form) is { } helmet)
            {
                Vector3 size = HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn).MeshAt(rot).bounds.size;
                Vector3 head = root + pawn.Drawer.renderer.BaseHeadOffsetAt(rot);
                DrawPiece(pawn, helmet, rot, head + new Vector3(0f, HelmetHeight, 0f), size, shown);
            }
        }
    }

    // The top `shown` of the piece's worn texture, plus the glowing seam along its bottom edge.
    private static void DrawPiece(Pawn pawn, ThingDef apparelDef, Rot4 rot, Vector3 pos, Vector3 meshSize, float shown)
    {
        if (!samples.TryGetValue(apparelDef, out Apparel? sample))
        {
            sample = (Apparel)ThingMaker.MakeThing(apparelDef, GenStuff.DefaultStuffFor(apparelDef));
            samples[apparelDef] = sample;
        }
        if (!ApparelGraphicRecordGetter.TryGetGraphicApparel(sample, pawn.story.bodyType, false, out ApparelGraphicRecord rec))
        {
            return;
        }
        Graphic graphic = rec.graphic;
        Material material = graphic.MatAt(rot);
        bool flip = (rot == Rot4.West && graphic.WestFlipped) || (rot == Rot4.East && graphic.EastFlipped);
        Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(meshSize.x, 1f, meshSize.z));
        Graphics.DrawMesh(FormMeshes.Revealed(shown, flip), matrix, material, 0);

        // The seam glows in the piece's own silhouette, flickers, and fades in and out at the ends of each phase.
        float flicker = 0.75f + 0.25f * Mathf.Sin(Time.realtimeSinceStartup * 30f);
        float alpha = flicker * Mathf.Clamp01(Mathf.Min(shown, 1f - shown) * 8f);
        if (alpha <= 0f || material.mainTexture is not Texture2D tex)
        {
            return;
        }
        Material seam = FadedMaterialPool.FadedVersionOf(MaterialPool.MatFrom(new MaterialRequest(tex, ShaderDatabase.MoteGlow, SeamColor)), alpha);
        Mesh seamMesh = FormMeshes.Seam(shown, flip);
        pos.y += SeamLift;
        matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(meshSize.x, 1f, meshSize.z));
        // Twice, as the glow shader is additive and one pass is faint over a dark suit.
        Graphics.DrawMesh(seamMesh, matrix, seam, 0);
        Graphics.DrawMesh(seamMesh, matrix, seam, 0);
    }
}

// Unit planes (1 x 1 on XZ, like MeshPool.plane10) cut to a horizontal strip, with the texture cut the same way so it
// does not stretch. Cached per step, so pawns forming at the same time can share them.
public static class FormMeshes
{
    private const int Steps = 60;

    // Half the height of the glowing seam, as a fraction of the texture.
    private const float SeamHalf = 0.035f;

    private static readonly Mesh?[,] revealed = new Mesh?[2, Steps + 1];
    private static readonly Mesh?[,] seams = new Mesh?[2, Steps + 1];

    // The top of the texture down to `progress` of its height.
    public static Mesh Revealed(float progress, bool flip)
    {
        int step = Step(progress);
        float cut = 1f - step / (float)Steps;
        return revealed[flip ? 1 : 0, step] ??= Strip(cut, 1f, flip);
    }

    // A thin band around the cut.
    public static Mesh Seam(float progress, bool flip)
    {
        int step = Step(progress);
        float cut = 1f - step / (float)Steps;
        return seams[flip ? 1 : 0, step] ??= Strip(Mathf.Max(0f, cut - SeamHalf), Mathf.Min(1f, cut + SeamHalf), flip);
    }

    private static int Step(float progress) => Mathf.RoundToInt(Mathf.Clamp01(progress) * Steps);

    // `bottom` and `top` are texture heights from 0 (bottom edge) to 1 (top edge).
    private static Mesh Strip(float bottom, float top, bool flip)
    {
        float zb = bottom - 0.5f;
        float zt = top - 0.5f;
        float u0 = flip ? 1f : 0f;
        float u1 = flip ? 0f : 1f;
        Mesh mesh = new()
        {
            name = "ExoskeletonForm",
            vertices = new[] { new Vector3(-0.5f, 0f, zb), new Vector3(-0.5f, 0f, zt), new Vector3(0.5f, 0f, zt), new Vector3(0.5f, 0f, zb) },
            uv = new[] { new Vector2(u0, bottom), new Vector2(u0, top), new Vector2(u1, top), new Vector2(u1, bottom) }
        };
        mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
