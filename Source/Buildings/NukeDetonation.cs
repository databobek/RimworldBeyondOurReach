using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class DefModExtension_Nuke : DefModExtension
{
    public float radius = 1000f;
    public int damage = 15000;

    // Below 0 uses the damage def's default.
    public float armorPenetration = -1f;
    public DamageDef? damageDef;
    public float chanceToStartFire = 0.1f;
    public float propagationSpeed = 1f;

    // A mountain roof shields what is under it, so mountain bases survive.
    public bool spareThickRoof = true;
    public SoundDef? sound;
    public float screenShakeFactor = 4f;

    // One distortion ring that grows with the front. Its fleck's growthRate must be 80 x propagationSpeed: the front moves
    // 40 cells/s in radius at speed 1, and growthRate is diameter per second.
    public FleckDef? ringFleck;

    // A band of distortion along the front, only where the camera is looking.
    public FleckDef? fringeFleck;
    public FloatRange fringeScale = new(0.8f, 1.4f);
    public float fringeSpacing = 4f;
    public float fringeJitter = 2.5f;
    public int fringeInterval = 4;
    public int fringeMaxPerPulse = 600;

    // Camera rumble while the front is on screen.
    public float fringeShake = 0.3f;
}

// A Cracker shell spawns this at the centre of its own blast, through preExplosionSpawnSingleThingDef, which both
// vanilla and CE explosions honor. Vanilla and CE explosions stop at the radial pattern (about radius 80) and need line of
// sight from the centre, so the shockwave builds its own cell list and hands it to a vanilla explosion as overrideCells.
// It then stays until the front has crossed the map, drawing the distortion at the front.
public class NukeDetonation : Thing
{
    private static readonly DefModExtension_Nuke Defaults = new();

    private int detonationTick = -1;

    // Distance from the centre to the farthest cell the shockwave reaches.
    private float reach;

    private DefModExtension_Nuke Ext => def.GetModExtension<DefModExtension_Nuke>() ?? Defaults;

    private int TicksSinceDetonation => Find.TickManager.TicksGame - detonationTick;

    // An explosion reaches a cell 1.5 ticks per cell of distance, divided by its propagation speed.
    private float FrontRadius => TicksSinceDetonation * Ext.propagationSpeed / 1.5f;

    protected override void Tick()
    {
        if (!Spawned)
        {
            return;
        }
        if (detonationTick < 0)
        {
            Detonate(Map, Position);
            return;
        }
        if (FrontRadius > reach + Ext.fringeSpacing)
        {
            Destroy();
            return;
        }
        if (Ext.fringeFleck != null && TicksSinceDetonation % Mathf.Max(1, Ext.fringeInterval) == 0)
        {
            DrawFringe();
        }
    }

    private void Detonate(Map map, IntVec3 center)
    {
        DefModExtension_Nuke ext = Ext;
        DamageDef damageDef = ext.damageDef ?? DamageDefOf.Bomb;

        // The shell's own explosion is still standing on this cell; it knows who fired.
        Explosion? source = center.GetThingList(map).OfType<Explosion>().FirstOrDefault();

        float radiusSquared = ext.radius * ext.radius;
        float reachSquared = 0f;
        List<IntVec3> cells = new();
        foreach (IntVec3 cell in map.AllCells)
        {
            float distanceSquared = (cell - center).LengthHorizontalSquared;
            if (distanceSquared > radiusSquared)
            {
                continue;
            }
            if (ext.spareThickRoof && (cell.GetRoof(map)?.isThickRoof ?? false))
            {
                continue;
            }
            cells.Add(cell);
            reachSquared = Mathf.Max(reachSquared, distanceSquared);
        }
        reach = Mathf.Sqrt(reachSquared);
        detonationTick = Find.TickManager.TicksGame;

        float armorPenetration = ext.armorPenetration >= 0f ? ext.armorPenetration : damageDef.defaultArmorPenetration;
        GenExplosion.DoExplosion(center, map, ext.radius, damageDef, source?.instigator, ext.damage, armorPenetration, ext.sound,
            source?.weapon, source?.projectile, null, chanceToStartFire: ext.chanceToStartFire, damageFalloff: true,
            doVisualEffects: false, propagationSpeed: ext.propagationSpeed, screenShakeFactor: ext.screenShakeFactor, overrideCells: cells);

        if (ext.ringFleck != null)
        {
            FleckCreationData ring = FleckMaker.GetDataStatic(center.ToVector3Shifted(), map, ext.ringFleck, 0.01f);
            ring.solidTimeOverride = reach * 1.5f / Mathf.Max(0.01f, ext.propagationSpeed) / 60f;
            map.flecks.CreateFleck(ring);
        }
    }

    private void DrawFringe()
    {
        DefModExtension_Nuke ext = Ext;
        float radius = FrontRadius;
        if (radius < 1f || Map != Find.CurrentMap || WorldRendererUtility.WorldRendered)
        {
            return;
        }
        CellRect view = Find.CameraDriver.CurrentViewRect.ExpandedBy(4);
        Vector3 center = Position.ToVector3Shifted();
        int count = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * radius / Mathf.Max(0.5f, ext.fringeSpacing)), 8, ext.fringeMaxPerPulse);
        float step = 2f * Mathf.PI / count;
        float offset = Rand.Range(0f, step);
        bool onScreen = false;
        for (int i = 0; i < count; i++)
        {
            float angle = offset + i * step;
            float distance = radius + Rand.Range(-ext.fringeJitter, ext.fringeJitter);
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            IntVec3 cell = point.ToIntVec3();
            if (!view.Contains(cell) || !cell.InBounds(Map))
            {
                continue;
            }
            FleckCreationData data = FleckMaker.GetDataStatic(point, Map, ext.fringeFleck, ext.fringeScale.RandomInRange);
            data.rotation = Rand.Range(0f, 360f);
            Map.flecks.CreateFleck(data);
            onScreen = true;
        }
        if (onScreen && ext.fringeShake > 0f)
        {
            Find.CameraDriver.shaker.DoShake(ext.fringeShake);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref detonationTick, "detonationTick", -1);
        Scribe_Values.Look(ref reach, "reach", 0f);
    }
}
