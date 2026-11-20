using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Drop pod that bursts into vomit, excrement and garbage where it lands.
public class Skyfaller_FilthPod : Skyfaller
{
    private static readonly string[] FilthDefs = { "Filth_Vomit", "Filth_AnimalFilth", "Filth_Trash", "Filth_Slime", "Filth_CorpseBile" };

    protected override void Impact()
    {
        if (Map != null)
        {
            int filth = Rand.RangeInclusive(50, 70);
            for (int i = 0; i < filth; i++)
            {
                IntVec3 c = Position + GenRadial.RadialPattern[Rand.Range(0, GenRadial.NumCellsInRadius(5.9f))];
                ThingDef? def = DefDatabase<ThingDef>.GetNamedSilentFail(FilthDefs.RandomElement());
                if (c.InBounds(Map) && def != null)
                {
                    FilthMaker.TryMakeFilth(c, Map, def, 3);
                }
            }
        }
        base.Impact();
    }
}

// Punishments scheduled when the shuttle pilot is refused or ignored.
public class MapComponent_SignalConsequences : MapComponent
{
    private const int CheckInterval = 250;

    private List<int> pendingTicks = new();

    public MapComponent_SignalConsequences(Map map) : base(map)
    {
    }

    public bool Pending => pendingTicks.Count > 0;

    public void Schedule()
    {
        pendingTicks.Add(Find.TickManager.TicksGame + Rand.RangeInclusive(2 * GenDate.TicksPerHour, 6 * GenDate.TicksPerHour));
    }

    public override void MapComponentTick()
    {
        if (pendingTicks.Count == 0 || Find.TickManager.TicksGame % CheckInterval != 0)
        {
            return;
        }
        int now = Find.TickManager.TicksGame;
        for (int i = pendingTicks.Count - 1; i >= 0; i--)
        {
            if (pendingTicks[i] <= now)
            {
                pendingTicks.RemoveAt(i);
                Fire(-1);
            }
        }
    }

    public void ClearPending() => pendingTicks.Clear();

    // forced: -1 rolls 20/50/30; 0 laser, 1 junk pods, 2 wastepacks (or psychic drone without Biotech).
    public void Fire(int forced)
    {
        float roll = forced switch
        {
            0 => 0f,
            1 => 0.5f,
            2 => 0.9f,
            _ => Rand.Value
        };
        if (roll < 0.2f)
        {
            OrbitalLaser();
        }
        else if (roll < 0.7f)
        {
            FilthPods();
        }
        else if (ModsConfig.BiotechActive)
        {
            Wastepacks();
        }
        else
        {
            PsychicDrone();
        }
    }

    private IntVec3 RandomHomeCell()
    {
        List<IntVec3> home = map.areaManager.Home.ActiveCells.Where(c => !c.Fogged(map)).ToList();
        if (home.Count > 0)
        {
            return home.RandomElement();
        }
        List<Building> buildings = map.listerBuildings.allBuildingsColonist;
        return buildings.Count > 0 ? buildings.RandomElement().Position : CellFinder.RandomNotEdgeCell(10, map);
    }

    private void OrbitalLaser()
    {
        IntVec3 cell = RandomHomeCell();
        PowerBeam beam = (PowerBeam)GenSpawn.Spawn(ThingDefOf.PowerBeam, cell, map);
        // A short zap, not the 10-second wandering beam of the vanilla permit.
        beam.duration = 45;
        beam.StartStrike();
        CameraJumper.TryJump(cell, map);
    }

    private void FilthPods()
    {
        IntVec3 first = IntVec3.Invalid;
        int pods = Rand.RangeInclusive(15, 25);
        for (int i = 0; i < pods; i++)
        {
            IntVec3 cell = SignalUtility.HomeDropSpot(map);
            SkyfallerMaker.SpawnSkyfaller(BOR_SignalsDefOf.BOR_FilthPodIncoming, ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel), cell, map);
            if (!first.IsValid)
            {
                first = cell;
            }
        }
        CameraJumper.TryJump(first, map);
    }

    private void Wastepacks()
    {
        ThingDef wastepack = DefDatabase<ThingDef>.GetNamed("Wastepack");
        const int total = 1000;
        const int batches = 20;
        IntVec3 first = IntVec3.Invalid;
        for (int b = 0; b < batches; b++)
        {
            List<Thing> things = new();
            for (int left = total / batches; left > 0; left -= wastepack.stackLimit)
            {
                Thing t = ThingMaker.MakeThing(wastepack);
                t.stackCount = Mathf.Min(left, wastepack.stackLimit);
                things.Add(t);
            }
            IntVec3 cell = SignalUtility.HomeDropSpot(map);
            DropPodUtility.DropThingsNear(cell, map, things);
            if (!first.IsValid)
            {
                first = cell;
            }
        }
        CameraJumper.TryJump(first, map);
    }

    private void PsychicDrone()
    {
        GameCondition_PsychicEmanation drone = (GameCondition_PsychicEmanation)GameConditionMaker.MakeCondition(
            GameConditionDefOf.PsychicDrone, Rand.RangeInclusive(GenDate.TicksPerHour * 12, GenDate.TicksPerDay * 3));
        drone.gender = Rand.Bool ? Gender.Male : Gender.Female;
        drone.level = PsychicDroneLevel.BadHigh;
        map.gameConditionManager.RegisterCondition(drone);
        CameraJumper.TryJump(map.Center, map);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref pendingTicks, "pendingTicks", LookMode.Value);
        pendingTicks ??= new List<int>();
    }
}
