using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// The dark mirror's answer to a broken pact: a red ring, like the Path of Light's golden one, racing across the map.
// Everyone it reaches, friend or foe, suffers Dark Punishment.
[StaticConstructorOnStartup]
public class DarkPulse : IExposable
{
    private const float TilesPerTick = 0.75f;

    private static readonly Material RingMaterial = MaterialPool.MatFrom("BOR/UI/Orientis/Shockwave", ShaderDatabase.MoteGlow, DarkPacts.DarkRed);

    private IntVec3 center;
    private int startTick;
    // Pawns already struck by this pulse, by id, so each is hit once.
    private HashSet<int> struck = new();

    // For loading.
    public DarkPulse()
    {
    }

    public DarkPulse(Map map, IntVec3 center)
    {
        this.center = center;
        startTick = Find.TickManager.TicksGame;
        Flash(map, center.ToVector3Shifted(), 6f);
        if (map == Find.CurrentMap)
        {
            Find.CameraDriver.shaker.DoShake(1.5f);
        }
    }

    private float Radius => (Find.TickManager.TicksGame - startTick) * TilesPerTick;

    // A red glow, for the pulse and the pact being made.
    public static void Flash(Map map, Vector3 pos, float scale)
    {
        FleckCreationData flash = FleckMaker.GetDataStatic(pos, map, FleckDefOf.LightningGlow, scale);
        flash.instanceColor = DarkPacts.DarkRed;
        map.flecks.CreateFleck(flash);
    }

    // True once the ring has crossed the whole map.
    public bool Tick(Map map)
    {
        float radius = Radius;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (!pawn.Dead && !struck.Contains(pawn.thingIDNumber) && pawn.Position.InHorDistOf(center, radius))
            {
                struck.Add(pawn.thingIDNumber);
                Punish(pawn);
            }
        }
        return radius >= ShockwaveRing.MaxRadius(map, center);
    }

    // A pawn already punished starts the day over.
    private static void Punish(Pawn pawn)
    {
        Hediff? existing = pawn.health.hediffSet.GetFirstHediffOfDef(BOR_DarkPactDefOf.BOR_DarkPunishment);
        if (existing != null)
        {
            existing.TryGetComp<HediffComp_Disappears>()?.ResetElapsedTicks();
            return;
        }
        pawn.health.AddHediff(BOR_DarkPactDefOf.BOR_DarkPunishment);
        Flash(pawn.Map, pawn.DrawPos, 1.2f);
    }

    public void Draw(Map map) => ShockwaveRing.Draw(map, center, Radius, RingMaterial);

    public void ExposeData()
    {
        Scribe_Values.Look(ref center, "center");
        Scribe_Values.Look(ref startTick, "startTick");
        Scribe_Collections.Look(ref struck, "struck", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            struck ??= new HashSet<int>();
        }
    }
}

public class MapComponent_DarkPulse : MapComponent
{
    private List<DarkPulse> pulses = new();

    public MapComponent_DarkPulse(Map map) : base(map)
    {
    }

    public void Emit(IntVec3 center) => pulses.Add(new DarkPulse(map, center));

    public override void MapComponentTick()
    {
        if (pulses.Count > 0)
        {
            pulses.RemoveAll(p => p.Tick(map));
        }
    }

    public override void MapComponentUpdate()
    {
        for (int i = 0; i < pulses.Count; i++)
        {
            pulses[i].Draw(map);
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref pulses, "darkPulses", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pulses ??= new List<DarkPulse>();
        }
    }
}
