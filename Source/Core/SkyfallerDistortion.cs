using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// For a leaving (reversed) skyfaller: small ripples in the air around it while it rises, then one big ripple at burstAt
// of the way through its departure, the moment it jumps away. Set the skyfaller's curves so the jump happens there.
// The flecks follow the drawn craft, including the height a zPositionCurve adds.
public class CompProperties_SkyfallerDistortion : CompProperties
{
    public float burstAt = 0.6f;
    public FleckDef? burstFleck;
    public float burstScale = 1f;
    public FleckDef? riseFleck;
    public float riseScale = 1f;
    public int riseIntervalTicks = 8;

    public CompProperties_SkyfallerDistortion()
    {
        compClass = typeof(CompSkyfallerDistortion);
    }
}

public class CompSkyfallerDistortion : ThingComp
{
    private bool burst;

    private CompProperties_SkyfallerDistortion Props => (CompProperties_SkyfallerDistortion)props;

    public override void CompTick()
    {
        if (burst || parent is not Skyfaller { Spawned: true } ship || !ship.def.skyfaller.reversed)
        {
            return;
        }
        float t = (float)ship.ticksToImpact / ship.LeaveMapAfterTicks;
        if (t < 0f)
        {
            return;
        }
        Vector3 pos = ship.DrawPos;
        if (ship.def.skyfaller.zPositionCurve != null)
        {
            pos.z += ship.def.skyfaller.zPositionCurve.Evaluate(t);
        }
        if (t >= Props.burstAt)
        {
            burst = true;
            Spawn(Props.burstFleck, pos, Props.burstScale);
        }
        else if (ship.IsHashIntervalTick(Props.riseIntervalTicks))
        {
            Spawn(Props.riseFleck, pos, Props.riseScale);
        }
    }

    private void Spawn(FleckDef? fleck, Vector3 pos, float scale)
    {
        if (fleck != null && pos.ShouldSpawnMotesAt(parent.Map))
        {
            FleckMaker.Static(pos, parent.Map, fleck, scale);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref burst, "distortionBurst");
    }
}
