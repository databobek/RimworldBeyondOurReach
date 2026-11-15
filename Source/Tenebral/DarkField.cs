using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// The tenebral dread beacon. Every pawn inside its red field builds dark influence: nothing at first, then
// confusion, then a rage. Psychic sensitivity sets the pace: 0% is immune, 100% rages after 60 s, 200% after 30 s.
public class CompProperties_DarkField : CompProperties
{
    public float radius = 15.9f;
    public HediffDef hediff = null!;

    // Seconds inside the field before dark rage, at 100% psychic sensitivity.
    public float secondsToRage = 60f;
    public float maxSensitivity = 2f;

    // Orientis pawns resist half of the field.
    public float orientisFactor = 0.5f;

    // Royalty's proximity-scanner ring, set by Mods/Royalty. Without it the comp draws a plain red circle.
    public ThingDef? ringMote;

    public CompProperties_DarkField() => compClass = typeof(CompDarkField);
}

public class CompDarkField : ThingComp
{
    private const int PulseInterval = 60;

    private CompPowerTrader? power;
    private Mote? ring;

    public CompProperties_DarkField Props => (CompProperties_DarkField)props;

    public bool Active => parent.Spawned && (power == null || power.PowerOn) && !parent.IsBrokenDown();

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
    }

    public override void CompTick()
    {
        base.CompTick();
        // An unmaintained ring fades out on its own, so switching the field off needs no cleanup.
        if (!Active)
        {
            return;
        }
        MaintainRing();
        if (parent.IsHashIntervalTick(PulseInterval))
        {
            Pulse();
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (Props.ringMote == null && Active)
        {
            Vector3 center = parent.TrueCenter();
            center.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            GenDraw.DrawCircleOutline(center, Props.radius, SimpleColor.Red);
        }
    }

    private void MaintainRing()
    {
        if (Props.ringMote == null)
        {
            return;
        }
        if (ring == null || ring.Destroyed)
        {
            ring = MoteMaker.MakeStaticMote(parent.TrueCenter(), parent.Map, Props.ringMote);
        }
        ring?.Maintain();
    }

    private void Pulse()
    {
        Vector3 center = parent.TrueCenter();
        float radiusSquared = Props.radius * Props.radius;
        float step = PulseInterval / 60f / Props.secondsToRage;
        foreach (Pawn pawn in parent.Map.mapPawns.AllPawnsSpawned.ToList())
        {
            if (pawn.Dead || (pawn.Position.ToVector3Shifted() - center).MagnitudeHorizontalSquared() > radiusSquared)
            {
                continue;
            }
            float effectiveness = DarkField.Effectiveness(pawn, Props);
            if (effectiveness <= 0f)
            {
                continue;
            }
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediff) ?? pawn.health.AddHediff(Props.hediff);
            (hediff as Hediff_DarkInfluence)?.Expose(step * effectiveness);
        }
    }
}

[DefOf]
public static class BOR_DarkFieldDefOf
{
    public static MentalStateDef WanderConfused = null!;

    static BOR_DarkFieldDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_DarkFieldDefOf));
    }
}

public static class DarkField
{
    // Tenebral psychosis and Umbral blood make a pawn immune; Orientis pawns take half.
    public static float Effectiveness(Pawn pawn, CompProperties_DarkField props)
    {
        if (TenebralPsychosis.IsPsychotic(pawn) || Umbral.IsUmbral(pawn))
        {
            return 0f;
        }
        float sensitivity = Mathf.Clamp(pawn.GetStatValue(StatDefOf.PsychicSensitivity), 0f, props.maxSensitivity);
        if (Orientis.IsOrientis(pawn))
        {
            sensitivity *= props.orientisFactor;
        }
        return sensitivity;
    }
}

// Severity 0 to 1. Dark mist at MistSeverity, dark rage at 1. Outside the field it falls at the base rate.
public class Hediff_DarkInfluence : HediffWithComps
{
    public const float MistSeverity = 0.33f;

    // 1/60 per second: a pawn at full influence is clear 60 s after leaving.
    private const float DecayPerTick = 1f / 3600f;

    // A pulse lands every 60 ticks, so anything longer than that means the pawn has left the field.
    private const int ExposureGraceTicks = 90;

    private int lastExposedTick = -99999;

    public void Expose(float amount)
    {
        lastExposedTick = Find.TickManager.TicksGame;
        float before = Severity;
        Severity = Mathf.Min(1f, Severity + amount);
        if (before < MistSeverity && Severity >= MistSeverity)
        {
            Confuse();
        }
        if (Severity >= 1f)
        {
            Rage();
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);
        if (Find.TickManager.TicksGame - lastExposedTick > ExposureGraceTicks)
        {
            Severity -= DecayPerTick * delta;
        }
    }

    private void Confuse()
    {
        if (!pawn.Spawned || pawn.Downed || pawn.InMentalState || pawn.RaceProps.IsMechanoid)
        {
            return;
        }
        pawn.mindState.mentalStateHandler.TryStartMentalState(BOR_DarkFieldDefOf.WanderConfused, "BOR_DarkMistReason".Translate(),
            forced: pawn.RaceProps.Humanlike, forceWake: true);
    }

    // A pawn already raging holds at full influence and rages again as soon as it ends, if it is still inside.
    private void Rage()
    {
        if (!pawn.Spawned || pawn.Downed)
        {
            return;
        }
        MentalStateDef rage = RageState(pawn);
        MentalStateDef? current = pawn.MentalStateDef;
        if (current == rage || (current != null && current.IsAggro))
        {
            return;
        }
        if (pawn.mindState.mentalStateHandler.TryStartMentalState(rage, "BOR_DarkRageReason".Translate(), forced: true, forceWake: true))
        {
            Severity = MistSeverity + 0.01f;
        }
    }

    private static MentalStateDef RageState(Pawn pawn) =>
        pawn.RaceProps.IsMechanoid ? MentalStateDefOf.BerserkMechanoid
        : pawn.RaceProps.Animal ? MentalStateDefOf.Manhunter
        : MentalStateDefOf.Berserk;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref lastExposedTick, "lastExposedTick", -99999);
    }
}

public class PlaceWorker_DarkFieldRadius : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing? thing = null)
    {
        CompProperties_DarkField? props = def.GetCompProperties<CompProperties_DarkField>();
        if (props != null)
        {
            Vector3 trueCenter = GenThing.TrueCenter(center, rot, def.size, AltitudeLayer.MetaOverlays.AltitudeFor());
            GenDraw.DrawCircleOutline(trueCenter, props.radius, SimpleColor.Red);
        }
    }
}
