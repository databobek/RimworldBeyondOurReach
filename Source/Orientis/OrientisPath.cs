using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

public enum ColonyPath
{
    None,
    Light,
    Dark
}

// The colony's one choice of path, and the Path of Light sequence, which has to survive a save partway through.
public class GameComponent_OrientisPath : GameComponent
{
    private enum Stage
    {
        None,
        Stripping,
        Judgment
    }

    private const int AuraInterval = 8;

    private static readonly Color AuraColor = new(1f, 0.8f, 0.25f, 1f);

    private static readonly float[] AuraScales = { 6f, 3f };

    private ColonyPath path;
    private bool letterSent;
    private Pawn? chooser;
    private Stage stage;
    private int stageStart;
    private int beamEnd;
    private bool partsStripped;
    private List<Pawn> stripped = new();
    private List<Pawn> glowing = new();
    private int glowEnd;

    public GameComponent_OrientisPath(Game game)
    {
    }

    public static GameComponent_OrientisPath Instance => Current.Game.GetComponent<GameComponent_OrientisPath>();

    public ColonyPath Path => path;

    public void OfferChoice(Pawn pawn)
    {
        if (path != ColonyPath.None || letterSent)
        {
            return;
        }
        letterSent = true;
        chooser = pawn;
        Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("BOR_OrientisChoiceLabel".Translate(), "BOR_OrientisChoiceText".Translate(),
            BOR_OrientisDefOf.BOR_OrientisChoice, pawn));
    }

    public void ChooseDark() => path = ColonyPath.Dark;

    public void ChooseLight()
    {
        path = ColonyPath.Light;
        stripped = Orientis.ColonyPawns().Where(Orientis.HasTenebralParts).ToList();
        stage = Stage.Stripping;
        stageStart = Find.TickManager.TicksGame;
        partsStripped = false;
        glowing = stripped.ToList();
        if (chooser != null && !glowing.Contains(chooser) && !Umbral.IsUmbral(chooser))
        {
            glowing.Add(chooser);
        }
        glowEnd = stageStart + Orientis.Settings.glowTicks;
        // The sequence runs on game ticks, so a paused game would show nothing at all.
        if (Find.TickManager.Paused)
        {
            Find.TickManager.TogglePaused();
        }
        Messages.Message("BOR_OrientisLightBegins".Translate(), MessageTypeDefOf.NeutralEvent);
    }

    // Forgets the choice so the letter can be tried again. Debug only.
    public void ResetChoice()
    {
        path = ColonyPath.None;
        letterSent = false;
        chooser = null;
        stage = Stage.None;
        stripped.Clear();
        glowing.Clear();
    }

    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        // Outlasts the sequence: the pawns are still glowing when the last letter arrives.
        if (glowing.Count > 0)
        {
            if (now >= glowEnd)
            {
                glowing.Clear();
            }
            else if (now % AuraInterval == 0)
            {
                DrawAuras();
            }
        }
        switch (stage)
        {
            case Stage.Stripping:
                int elapsed = now - stageStart;
                if (!partsStripped && elapsed >= Orientis.Settings.stripTicks / 2)
                {
                    partsStripped = true;
                    StripAll();
                }
                if (elapsed >= Orientis.Settings.stripTicks)
                {
                    Judge(now);
                }
                break;
            case Stage.Judgment:
                if (now >= beamEnd)
                {
                    Finish();
                }
                break;
        }
    }

    private void DrawAuras()
    {
        foreach (Pawn pawn in glowing)
        {
            if (pawn.Spawned)
            {
                // A wide halo around a bright core.
                foreach (float scale in AuraScales)
                {
                    FleckCreationData data = FleckMaker.GetDataStatic(pawn.DrawPos, pawn.Map, FleckDefOf.LightningGlow, scale);
                    data.instanceColor = AuraColor;
                    pawn.Map.flecks.CreateFleck(data);
                }
            }
        }
    }

    private void StripAll()
    {
        foreach (Map map in Find.Maps)
        {
            foreach (Building_TenebralPod pod in map.listerBuildings.allBuildingsColonist.OfType<Building_TenebralPod>().ToList())
            {
                if (pod.Transforming && pod.Occupant?.Faction == Faction.OfPlayer)
                {
                    pod.DestroyWithOccupant();
                }
            }
        }
        foreach (Pawn pawn in stripped)
        {
            if (!pawn.Dead)
            {
                Orientis.Strip(pawn);
            }
        }
    }

    // Nothing but the Umbral pawn is hurt, and only those on a map get a beam to look at. Dark altars are struck dead the same way.
    private void Judge(int now)
    {
        List<Building> altars = Find.Maps.SelectMany(m => m.listerBuildings.allBuildingsColonist).Where(b => b.TryGetComp<CompDarkAltar>() != null).ToList();
        foreach (Building altar in altars)
        {
            altar.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_GoldenBeam(altar.Map, altar.Position));
            altar.TryGetComp<CompDarkAltar>().ClearComponents();
            DarkBuildings.ToHusk(altar);
        }
        List<Pawn> umbrals = Orientis.ColonyPawns().Where(Umbral.IsUmbral).ToList();
        foreach (Pawn pawn in umbrals)
        {
            if (pawn.Spawned)
            {
                pawn.Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_GoldenBeam(pawn.Map, pawn.Position));
            }
            pawn.Kill(null);
        }
        stage = Stage.Judgment;
        beamEnd = umbrals.Count + altars.Count > 0 ? now + Orientis.Settings.beamTicks : now;
    }

    private void Finish()
    {
        stage = Stage.None;
        if (chooser != null && !stripped.Contains(chooser) && chooser.Faction == Faction.OfPlayer)
        {
            stripped.Add(chooser);
        }
        foreach (Pawn pawn in stripped)
        {
            if (!pawn.Dead && !Umbral.IsUmbral(pawn))
            {
                Orientis.Convert(pawn);
            }
        }
        stripped.Clear();
        DarkBuildings.ShutDown();
        foreach (Pawn pawn in Orientis.ColonyPawns())
        {
            if (Orientis.IsOrientis(pawn))
            {
                Orientis.GrantManifest(pawn);
            }
        }
        Find.LetterStack.ReceiveLetter("BOR_OrientisDoneLabel".Translate(), "BOR_OrientisDoneText".Translate(), LetterDefOf.PositiveEvent);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref path, "orientisPath");
        Scribe_Values.Look(ref letterSent, "orientisLetterSent");
        Scribe_References.Look(ref chooser, "orientisChooser");
        Scribe_Values.Look(ref stage, "orientisStage");
        Scribe_Values.Look(ref stageStart, "orientisStageStart");
        Scribe_Values.Look(ref beamEnd, "orientisBeamEnd");
        Scribe_Values.Look(ref partsStripped, "orientisPartsStripped");
        Scribe_Collections.Look(ref stripped, "orientisStripped", LookMode.Reference);
        Scribe_Collections.Look(ref glowing, "orientisGlowing", LookMode.Reference);
        Scribe_Values.Look(ref glowEnd, "orientisGlowEnd");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            stripped ??= new List<Pawn>();
            stripped.RemoveAll(p => p == null);
            glowing ??= new List<Pawn>();
            glowing.RemoveAll(p => p == null);
        }
    }
}

// Has to be answered: it never times out and a right click does not dismiss it.
public class ChoiceLetter_OrientisPath : ChoiceLetter
{
    public override bool CanDismissWithRightClick => false;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            yield return new DiaOption("BOR_PathOfLight".Translate())
            {
                action = () =>
                {
                    Find.LetterStack.RemoveLetter(this);
                    GameComponent_OrientisPath.Instance.ChooseLight();
                },
                resolveTree = true
            };
            DiaNode response = new("BOR_PathOfDarkResponse".Translate());
            response.options.Add(new DiaOption("OK".Translate()) { resolveTree = true });
            yield return new DiaOption("BOR_PathOfDark".Translate())
            {
                action = () =>
                {
                    Find.LetterStack.RemoveLetter(this);
                    GameComponent_OrientisPath.Instance.ChooseDark();
                },
                link = response
            };
            if (lookTargets.IsValid())
            {
                yield return Option_JumpToLocation;
            }
        }
    }
}

// A golden beam from the sky, purely for the eyes. The kill is done by the Path of Light sequence.
[StaticConstructorOnStartup]
public class WeatherEvent_GoldenBeam : WeatherEvent
{
    private const int Duration = 120;
    private const float Width = 3f;
    private const float Length = 60f;
    private const int GroundEffectInterval = 8;

    private static readonly Color BeamColor = new(1f, 0.82f, 0.3f);
    private static readonly Material BeamMaterial = MaterialPool.MatFrom("Other/OrbitalBeam", ShaderDatabase.MoteGlow, BeamColor);
    private static readonly Material BeamEndMaterial = MaterialPool.MatFrom("Other/OrbitalBeamEnd", ShaderDatabase.MoteGlow, BeamColor);

    private readonly IntVec3 strikeLoc;
    private int age;

    public WeatherEvent_GoldenBeam(Map map, IntVec3 strikeLoc) : base(map)
    {
        this.strikeLoc = strikeLoc;
    }

    public override bool Expired => age > Duration;

    public override void FireEvent()
    {
        Vector3 loc = strikeLoc.ToVector3Shifted();
        for (int i = 0; i < 4; i++)
        {
            FleckCreationData data = FleckMaker.GetDataStatic(loc, map, FleckDefOf.LightningGlow, 6f);
            data.instanceColor = BeamColor;
            map.flecks.CreateFleck(data);
        }
        SoundDefOf.Thunder_OnMap.PlayOneShot(SoundInfo.InMap(new TargetInfo(strikeLoc, map)));
    }

    public override void WeatherEventTick()
    {
        age++;
        if (age % GroundEffectInterval == 0)
        {
            Vector3 loc = strikeLoc.ToVector3Shifted();
            FleckCreationData glow = FleckMaker.GetDataStatic(loc, map, FleckDefOf.LightningGlow, Width * 1.5f);
            glow.instanceColor = BeamColor;
            map.flecks.CreateFleck(glow);
            FleckMaker.ThrowDustPuffThick(loc + Gen.RandomHorizontalVector(Width / 2f), map, 2f, BeamColor);
            FleckMaker.ThrowMicroSparks(loc, map);
        }
    }

    public override void WeatherEventDraw()
    {
        // The beam runs up from the target to the top of the screen, and vanilla's soft end sits at the target.
        float alpha = 1f - age / (float)Duration;
        float endHeight = Width / 2f;
        Vector3 target = strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather);
        Matrix4x4 beam = Matrix4x4.TRS(target + Vector3.forward * ((endHeight + Length) / 2f), Quaternion.identity, new Vector3(Width, 1f, Length));
        Graphics.DrawMesh(MeshPool.plane10, beam, FadedMaterialPool.FadedVersionOf(BeamMaterial, alpha), 0);
        Matrix4x4 end = Matrix4x4.TRS(target, Quaternion.identity, new Vector3(Width, 1f, endHeight));
        Graphics.DrawMesh(MeshPool.plane10, end, FadedMaterialPool.FadedVersionOf(BeamEndMaterial, alpha), 0);
    }
}

public static class OrientisDebug
{
    [DebugAction("Pawns", "Send Orientis choice", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void SendChoice(Pawn pawn)
    {
        GameComponent_OrientisPath.Instance.ResetChoice();
        GameComponent_OrientisPath.Instance.OfferChoice(pawn);
    }

    // Makes the pawn psychotic if it is not yet, then starts a berserk, which the break counter counts.
    [DebugAction("Pawns", "Force tenebral break", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void ForceBreak(Pawn pawn)
    {
        if (!pawn.RaceProps.Humanlike)
        {
            return;
        }
        if (!TenebralPsychosis.IsPsychotic(pawn))
        {
            Hediff psychosis = pawn.health.GetOrAddHediff(BOR_TenebralDefOf.BOR_TenebralPsychosis);
            psychosis.Severity = TenebralPsychosis.PsychoticSeverity + 0.1f;
        }
        pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, forceWake: true);
    }

    [DebugAction("Pawns", "Make Orientis", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static void MakeOrientis(Pawn pawn)
    {
        if (pawn.RaceProps.Humanlike)
        {
            Orientis.Convert(pawn);
            Orientis.GrantManifest(pawn);
        }
    }
}
