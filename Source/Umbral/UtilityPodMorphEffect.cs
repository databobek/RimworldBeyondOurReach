using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// While a utility pod changes form the hand is empty, and the crystal is drawn where the weapon would be, morphing like a
// slide transition: one outline slides from the old shape's visible bounds to the new shape's, both textures are stretched
// to fit it, and the new shape fades in under the old one as the old one fades out.
// Mods move weapons (SQUISHY holds guns further out and turns them, but not melee weapons), so each shape's pose is taken
// from the real weapon renderer and the morph slides from the old shape's pose to the new one's. The real weapon then
// appears exactly where the morph ended.
public static class UtilityPodMorphs
{
    private static readonly HashSet<HediffComp_UmbralUtilityPod> active = new();

    // Finished morphs, removed pods, dead pawns and comps left over from another game.
    private static readonly Predicate<HediffComp_UmbralUtilityPod> stale =
        c => !c.Morphing || c.Pawn == null || c.Pawn.Dead || c.Pawn.Destroyed;

    // The fading-out shape goes just above the fading-in one.
    private static readonly float OldLift = PawnRenderUtility.AltitudeForLayer(0.1f);

    // Visible part of each texture, in UV (0..1, v up), from its opaque pixels.
    private static readonly Dictionary<Texture2D, Rect> contentBounds = new();
    private const int BoundsResolution = 128;
    private const byte OpaqueAlpha = 25;

    // A pose request in flight: the proxy whose DrawEquipmentAiming call is caught, and what it arrived with.
    private static Thing? capturing;
    private static bool captured;
    private static Vector3 capturedLoc;
    private static float capturedAim;

    // The game the set belongs to. Loading another save drops the old game's comps; morphing comps re-add themselves on tick.
    private static Game? activeGame;

    // A render error is reported once and the effect turned off, instead of throwing every frame.
    private static bool disabled;

    public static void Add(HediffComp_UmbralUtilityPod comp)
    {
        SyncGame();
        active.Add(comp);
    }

    private static void SyncGame()
    {
        if (activeGame != Current.Game)
        {
            active.Clear();
            activeGame = Current.Game;
        }
    }

    // Called from the lowest-priority DrawEquipmentAiming prefix, after every mod has moved the weapon. True stops the draw.
    public static bool TryCapture(Thing eq, Vector3 drawLoc, float aimAngle)
    {
        if (capturing == null || eq != capturing)
        {
            return false;
        }
        captured = true;
        capturedLoc = drawLoc;
        capturedAim = aimAngle;
        return true;
    }

    public static void Draw(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
    {
        if (active.Count == 0 || disabled)
        {
            return;
        }
        try
        {
            DrawMorph(pawn, drawPos, facing, flags);
        }
        catch (Exception ex)
        {
            disabled = true;
            capturing = null;
            Log.Error("[BOR] Utility pod morph effect disabled after error: " + ex);
        }
    }

    private static void DrawMorph(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
    {
        SyncGame();
        active.RemoveWhere(stale);
        HediffComp_UmbralUtilityPod? pod = null;
        foreach (HediffComp_UmbralUtilityPod comp in active)
        {
            if (comp.Pawn == pawn)
            {
                pod = comp;
                break;
            }
        }
        if (pod == null || !pod.TryGetMorphFrame(out ThingDef? from, out ThingDef? to, out float t) || from == null || to == null)
        {
            return;
        }
        if (pawn.CurJob == null || pawn.CurJob.def.neverShowWeapon)
        {
            return;
        }
        if (from.graphic?.MatSingle?.mainTexture is not Texture2D fromTex || to.graphic?.MatSingle?.mainTexture is not Texture2D toTex)
        {
            return;
        }
        ThingWithComps? fromProxy = pod.PoseProxy(from);
        ThingWithComps? toProxy = pod.PoseProxy(to);
        if (fromProxy == null || toProxy == null
            || !TryGetPose(pawn, fromProxy, drawPos, facing, flags, out Vector3 fromLoc, out float fromAim)
            || !TryGetPose(pawn, toProxy, drawPos, facing, flags, out Vector3 toLoc, out float toAim))
        {
            return;
        }

        float eased = Mathf.SmoothStep(0f, 1f, t);
        MeshAngle(fromAim, from.equippedAngleOffset, out bool fromFlip, out float fromAngle);
        MeshAngle(toAim, to.equippedAngleOffset, out bool toFlip, out float toAngle);
        bool flip = eased < 0.5f ? fromFlip : toFlip;
        Quaternion rotation = Quaternion.AngleAxis(Mathf.LerpAngle(fromAngle, toAngle, eased), Vector3.up);
        Mesh mesh = flip ? MeshPool.plane10Flip : MeshPool.plane10;
        Vector3 pos = Vector3.Lerp(fromLoc, toLoc, eased);

        // The shared outline, in world units around the weapon's center, along the texture's axes.
        Rect fromBounds = ContentBounds(fromTex);
        Rect toBounds = ContentBounds(toTex);
        Vector2 fromSize = from.graphic.drawSize;
        Vector2 toSize = to.graphic.drawSize;
        Vector2 center = Vector2.Lerp(Scale(fromBounds.center - Half, fromSize), Scale(toBounds.center - Half, toSize), eased);
        Vector2 size = Vector2.Lerp(Scale(fromBounds.size, fromSize), Scale(toBounds.size, toSize), eased);

        // Overlapping fades, so the shape never goes see-through in the middle.
        float newAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.6f, t));
        float oldAlpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 1f, t));
        DrawFitted(toTex, toBounds, center, size, newAlpha, pos, rotation, mesh, flip);
        pos.y += OldLift;
        DrawFitted(fromTex, fromBounds, center, size, oldAlpha, pos, rotation, mesh, flip);
    }

    // Where the renderer, with every mod's changes, would put this weapon in the pawn's hands this frame. Mirrors
    // PawnRenderUtility.DrawEquipmentAndApparelExtras: aimed while the pawn is busy with a target, else carried if shown.
    private static bool TryGetPose(Pawn pawn, ThingWithComps proxy, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags,
        out Vector3 loc, out float aim)
    {
        loc = default;
        aim = 0f;
        float distance = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;
        captured = false;
        capturing = proxy;
        try
        {
            if (!flags.HasFlag(PawnRenderFlags.NeverAimWeapon) && pawn.stances?.curStance is Stance_Busy { neverAimWeapon: false } busy
                && busy.focusTarg.IsValid)
            {
                Vector3 target = busy.focusTarg.HasThing ? busy.focusTarg.Thing.DrawPos : busy.focusTarg.Cell.ToVector3Shifted();
                float angle = (target - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f ? (target - pawn.DrawPos).AngleFlat() : 0f;
                Vector3 held = drawPos + new Vector3(0f, 0f, 0.4f + proxy.def.equippedDistanceOffset).RotatedBy(angle) * distance;
                PawnRenderUtility.DrawEquipmentAiming(proxy, held, angle);
            }
            else if (PawnRenderUtility.CarryWeaponOpenly(pawn))
            {
                PawnRenderUtility.DrawCarriedWeapon(proxy, drawPos, facing, distance);
            }
        }
        finally
        {
            capturing = null;
        }
        if (!captured)
        {
            return false;
        }
        loc = capturedLoc;
        aim = capturedAim;
        return true;
    }

    // The mesh and rotation DrawEquipmentAiming uses for an aim angle.
    private static void MeshAngle(float aim, float angleOffset, out bool flip, out float angle)
    {
        flip = aim > 200f && aim < 340f;
        angle = (flip ? aim - 270f - angleOffset : aim - 90f + angleOffset) % 360f;
    }

    private static readonly Vector2 Half = new(0.5f, 0.5f);

    private static Vector2 Scale(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);

    // Draws the texture stretched so its visible part fills the outline (center and size around the weapon's center).
    private static void DrawFitted(Texture2D tex, Rect uv, Vector2 center, Vector2 size, float alpha, Vector3 pos, Quaternion rotation,
        Mesh mesh, bool flip)
    {
        if (alpha <= 0.01f)
        {
            return;
        }
        Vector2 quad = new(size.x / Mathf.Max(0.01f, uv.width), size.y / Mathf.Max(0.01f, uv.height));
        Vector2 quadCenter = center - Scale(uv.center - Half, quad);
        // The flipped mesh mirrors the texture across its width.
        Vector3 offset = rotation * new Vector3(flip ? -quadCenter.x : quadCenter.x, 0f, quadCenter.y);
        Matrix4x4 matrix = Matrix4x4.TRS(pos + offset, rotation, new Vector3(quad.x, 1f, quad.y));
        Material mat = FadedMaterialPool.FadedVersionOf(MaterialPool.MatFrom(new MaterialRequest(tex, ShaderDatabase.Transparent)), alpha);
        Graphics.DrawMesh(mesh, matrix, mat, 0);
    }

    // Game textures are not readable, so they are copied through a small render texture once and the alpha scanned.
    private static Rect ContentBounds(Texture2D tex)
    {
        if (contentBounds.TryGetValue(tex, out Rect bounds))
        {
            return bounds;
        }
        const int n = BoundsResolution;
        RenderTexture rt = RenderTexture.GetTemporary(n, n, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        Texture2D read = new(n, n, TextureFormat.ARGB32, false);
        read.ReadPixels(new Rect(0, 0, n, n), 0, 0);
        read.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        Color32[] pixels = read.GetPixels32();
        UnityEngine.Object.Destroy(read);

        int minX = n, minY = n, maxX = -1, maxY = -1;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                if (pixels[y * n + x].a > OpaqueAlpha)
                {
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }
        bounds = maxX < 0 ? new Rect(0f, 0f, 1f, 1f) : Rect.MinMaxRect(minX / (float)n, minY / (float)n, (maxX + 1) / (float)n, (maxY + 1) / (float)n);
        contentBounds[tex] = bounds;
        return bounds;
    }
}

// Both pawn draw paths (the render tree up close, the cached frame zoomed out) draw the held weapon through here.
// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
[HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
public static class PawnRenderUtility_DrawEquipmentAndApparelExtras_UtilityPodMorph_Patch
{
    public static void Postfix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
    {
        // A pawn lying down holds no weapon, so there is nothing to morph in its hands.
        if (pawn.equipment == null || pawn.equipment.Primary != null || pawn.Dead || pawn.Downed
            || pawn.GetPosture() != PawnPosture.Standing)
        {
            return;
        }
        UtilityPodMorphs.Draw(pawn, drawPos, facing, flags);
    }
}

// Runs after every other mod's prefix has moved the weapon, so a pose proxy is caught at its final place and not drawn.
[HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming))]
public static class PawnRenderUtility_DrawEquipmentAiming_UtilityPodPose_Patch
{
    [HarmonyPriority(Priority.Last)]
    public static bool Prefix(Thing eq, Vector3 drawLoc, float aimAngle) => !UtilityPodMorphs.TryCapture(eq, drawLoc, aimAngle);
}
