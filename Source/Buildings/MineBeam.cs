using CVN_EnhancedResourceDrill;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

public class CompProperties_MineBeam : CompProperties
{
    private const int BeamMeshSteps = 32;

    // Same size as the building's texture. The glow goes under the rocks, the beam over them.
    public string glowTexPath = "";
    public string rockTexPath = "";
    public string beamTexPath = "";

    // Plays for as long as any of the beam is out.
    public SoundDef? beamSound;

    public int extendTicks = 150;
    public int retractTicks = 150;
    public int pulseTicks = 96;
    public float pulseStrength = 0.4f;

    // How much of the extend goes to fading the ball in, before the line starts to uncover.
    public float ballShare = 0.25f;

    // Each pass adds the glow texture's light on top once more, for a brighter glow.
    public int glowPasses = 2;

    // Positions in the textures, from 0 at the top to 1 at the bottom.
    public float glowCenter = 0.565f;
    public float ballBottom = 0.576f;
    public float beamBottom = 0.667f;

    // The props are shared by every mine of a def, so they hold the meshes only once.
    private Mesh?[]? beamMeshes;

    public CompProperties_MineBeam()
    {
        compClass = typeof(CompMineBeam);
    }

    // The top of the plane down to the cut, so a shorter mesh uncovers less of the line. 0 is only the ball.
    public Mesh BeamMesh(float reveal)
    {
        beamMeshes ??= new Mesh?[BeamMeshSteps + 1];
        int step = Mathf.RoundToInt(Mathf.Clamp01(reveal) * BeamMeshSteps);
        return beamMeshes[step] ??= NewMeshCutAt(Mathf.Lerp(ballBottom, beamBottom, step / (float)BeamMeshSteps));
    }

    private static Mesh NewMeshCutAt(float fromTop)
    {
        float v = 1f - fromTop;
        float z = v - 0.5f;
        Mesh mesh = new()
        {
            name = "MineBeam",
            vertices = new[] { new Vector3(-0.5f, 0f, z), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, z) },
            uv = new[] { new Vector2(0f, v), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, v) }
        };
        mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

public sealed class CompMineBeam : ThingComp
{
    // The printed building texture slopes up by 0.01 towards its top edge, so anything drawn over it has to start above that.
    private const float GlowHeight = 0.012f;
    private const float RockHeight = 0.014f;
    private const float BeamHeight = 0.016f;
    private const float GlowPulseGrowth = 0.08f;

    // One queue for all three, because the glow's shader would otherwise draw after the rocks whatever its height. Within a
    // queue, transparent draws go back to front by height. This is after building edge shadows (2950) and before motes (2990).
    private const int RenderQueue = 2960;

    private CompResourceDrill? drill;
    private Material? glowMaterial;
    private Material? rockMaterial;
    private Material? beamMaterial;
    private Sustainer? sustainer;

    // 0 is retracted and 1 is fully out; the draw works everything else out from it, so retracting is extending in reverse.
    private float progress;

    public CompProperties_MineBeam Props => (CompProperties_MineBeam)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        drill = parent.GetComp<CompResourceDrill>();
    }

    public override void CompTick()
    {
        base.CompTick();
        // The drill starts the next queued action in the same tick the last one ends, so this stays true across the whole queue.
        if (drill is { CanOperate: true })
        {
            progress = Mathf.Min(1f, progress + 1f / Props.extendTicks);
        }
        else if (progress > 0f)
        {
            progress = Mathf.Max(0f, progress - 1f / Props.retractTicks);
        }

        if (progress > 0f)
        {
            if (sustainer == null || sustainer.Ended)
            {
                sustainer = Props.beamSound?.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTick));
            }
            sustainer?.Maintain();
        }
        else
        {
            EndSound();
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        EndSound();
    }

    private void EndSound()
    {
        if (sustainer is { Ended: false })
        {
            sustainer.End();
        }
        sustainer = null;
    }

    public override void PostDraw()
    {
        base.PostDraw();
        glowMaterial ??= MaterialPool.MatFrom(Props.glowTexPath, ShaderDatabase.MoteGlow);
        rockMaterial ??= MaterialPool.MatFrom(Props.rockTexPath, ShaderDatabase.Transparent);
        beamMaterial ??= MaterialPool.MatFrom(Props.beamTexPath, ShaderDatabase.MoteGlow);

        Vector3 center = parent.DrawPos + parent.Graphic.DrawOffset(parent.Rotation);
        Vector2 size = parent.Graphic.drawSize;

        float ball = Mathf.SmoothStep(0f, 1f, progress / Props.ballShare);
        float line = Mathf.SmoothStep(0f, 1f, (progress - Props.ballShare) / (1f - Props.ballShare));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Find.TickManager.TicksGame * 2f * Mathf.PI / Props.pulseTicks);
        float fade = 1f - Props.pulseStrength * (1f - pulse);

        if (line > 0f)
        {
            float scale = line * (1f + GlowPulseGrowth * Props.pulseStrength * pulse);
            // Scaled around the middle of the glow, which isn't the middle of its texture.
            float pivot = (0.5f - Props.glowCenter) * size.y;
            Vector3 glowPos = center + new Vector3(0f, GlowHeight, pivot * (1f - scale));
            Matrix4x4 glowMatrix = Matrix4x4.TRS(glowPos, Quaternion.identity, new Vector3(size.x * scale, 1f, size.y * scale));
            Material faded = FadedMaterialPool.FadedVersionOf(glowMaterial, line * fade);
            for (int i = 0; i < Props.glowPasses; i++)
            {
                Draw(MeshPool.plane10, glowMatrix, faded);
            }
        }

        Matrix4x4 LayerMatrix(float height) => Matrix4x4.TRS(center + new Vector3(0f, height, 0f), Quaternion.identity, new Vector3(size.x, 1f, size.y));

        Draw(MeshPool.plane10, LayerMatrix(RockHeight), rockMaterial);

        if (ball > 0f)
        {
            Draw(Props.BeamMesh(line), LayerMatrix(BeamHeight), FadedMaterialPool.FadedVersionOf(beamMaterial, ball * fade));
        }
    }

    private static void Draw(Mesh mesh, Matrix4x4 matrix, Material material)
    {
        Graphics.DrawMesh(mesh, matrix, material, 0, null!, 0, (MaterialPropertyBlock)null!, UnityEngine.Rendering.ShadowCastingMode.Off, false, RenderQueue);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref progress, "mineBeamProgress");
    }
}
