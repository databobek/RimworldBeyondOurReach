using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
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

    private const float HeraldDelaySeconds = 3f;

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
    private List<LightShockwave> shockwaves = new();
    private int glowEnd;
    private int heraldLetterTick = -1;
    private bool heraldArmed;
    private float heraldStrikeTime;

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
        Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("BOR_OrientisChoiceLabel".Translate(), "BOR_OrientisChoiceText".Translate(pawn.LabelShort),
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
        glowing = Orientis.ColonyPawns().Where(p => Orientis.HasTenebralParts(p) || OrientisRestrictions.HasDarkGear(p)).ToList();
        if (chooser != null && !glowing.Contains(chooser) && !Umbral.IsUmbral(chooser))
        {
            glowing.Add(chooser);
        }
        glowEnd = stageStart + Orientis.Settings.glowTicks;
        // The sequence runs on game ticks, so a paused game would show nothing at all.
        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
        {
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        }
        Messages.Message("BOR_OrientisLightBegins".Translate(), MessageTypeDefOf.NeutralEvent);
        heraldLetterTick = stageStart + GenDate.TicksPerHour;
    }

    // The letter that sets this off has been closed; the strike follows on real time, so it comes even if paused.
    public void ArmHerald() => heraldArmed = true;

    private void UpdateHerald()
    {
        if (!heraldArmed)
        {
            return;
        }
        // Zero after a load too, so a reload just restarts the short wait.
        if (heraldStrikeTime == 0f)
        {
            heraldStrikeTime = Time.realtimeSinceStartup + HeraldDelaySeconds;
        }
        if (Time.realtimeSinceStartup < heraldStrikeTime)
        {
            return;
        }
        heraldArmed = false;
        heraldStrikeTime = 0f;
        if (Find.TickManager.CurTimeSpeed == TimeSpeed.Paused)
        {
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
        }
        UmbralHerald.Strike(chooser);
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
        shockwaves.Clear();
        heraldLetterTick = -1;
        heraldArmed = false;
        heraldStrikeTime = 0f;
    }

    public override void GameComponentUpdate()
    {
        foreach (LightShockwave wave in shockwaves)
        {
            wave.Draw();
        }
        UpdateHerald();
    }

    public override void GameComponentTick()
    {
        int now = Find.TickManager.TicksGame;
        shockwaves.RemoveAll(wave => wave.Tick());
        if (heraldLetterTick >= 0 && now >= heraldLetterTick)
        {
            heraldLetterTick = -1;
            Find.LetterStack.ReceiveLetter(LetterMaker.MakeLetter("BOR_DarkDispleasureLabel".Translate(), "BOR_DarkDispleasureText".Translate(), BOR_OrientisDefOf.BOR_DarkDispleasure));
        }
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
                if (now >= beamEnd && shockwaves.Count == 0)
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
        foreach (Pawn pawn in Orientis.ColonyPawns())
        {
            OrientisRestrictions.BurnDarkGear(pawn);
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
            shockwaves.Add(new LightShockwave(altar.Map, altar.Position, now));
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
        DarkBuildings.BurnAllItems();
        // The pawn who made the choice if it lived through it, else whoever did.
        Pawn? named = chooser != null && !chooser.Dead && Orientis.IsOrientis(chooser) ? chooser : Orientis.ColonyPawns().FirstOrDefault(Orientis.IsOrientis);
        string name = named?.LabelShort ?? "BOR_OrientisDoneFallback".Translate();
        Find.LetterStack.ReceiveLetter("BOR_OrientisDoneLabel".Translate(), "BOR_OrientisDoneText".Translate(name), LetterDefOf.PositiveEvent);
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
        Scribe_Collections.Look(ref shockwaves, "orientisShockwaves", LookMode.Deep);
        Scribe_Values.Look(ref glowEnd, "orientisGlowEnd");
        Scribe_Values.Look(ref heraldLetterTick, "orientisHeraldLetterTick", -1);
        Scribe_Values.Look(ref heraldArmed, "orientisHeraldArmed");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            stripped ??= new List<Pawn>();
            stripped.RemoveAll(p => p == null);
            glowing ??= new List<Pawn>();
            glowing.RemoveAll(p => p == null);
            shockwaves ??= new List<LightShockwave>();
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
            DiaNode lightResponse = new("BOR_PathOfLightResponse".Translate());
            lightResponse.options.Add(new DiaOption("OK".Translate()) { resolveTree = true });
            yield return new DiaOption("BOR_PathOfLight".Translate())
            {
                action = () =>
                {
                    Find.LetterStack.RemoveLetter(this);
                    GameComponent_OrientisPath.Instance.ChooseLight();
                },
                link = lightResponse
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
        }
    }
}

// A ring of light racing out from a struck altar. Dark buildings die and dark items burn the moment it reaches them.
[StaticConstructorOnStartup]
public class LightShockwave : IExposable
{
    private static readonly Material RingMaterial = MaterialPool.MatFrom("BOR/UI/Orientis/Shockwave", ShaderDatabase.MoteGlow);

    private Map map = null!;
    private IntVec3 center;
    private int startTick;
    private List<Thing>? items;

    // For loading.
    public LightShockwave()
    {
    }

    public LightShockwave(Map map, IntVec3 center, int startTick)
    {
        this.map = map;
        this.center = center;
        this.startTick = startTick;
        if (map == Find.CurrentMap)
        {
            Find.CameraDriver.shaker.DoShake(1.5f);
        }
    }

    private float Radius => (Find.TickManager.TicksGame - startTick) * Orientis.Settings.shockwaveTilesPerTick;

    // From the center to the farthest corner of the map.
    private float MaxRadius => Mathf.Sqrt(Square(Mathf.Max(center.x, map.Size.x - center.x)) + Square(Mathf.Max(center.z, map.Size.z - center.z)));

    private static float Square(float value) => value * value;

    // True once the wave has crossed the whole map, or the map is gone.
    public bool Tick()
    {
        if (map == null || !Find.Maps.Contains(map))
        {
            return true;
        }
        float radius = Radius;
        foreach (Building building in map.listerBuildings.allBuildingsColonist.Where(b => DarkBuildings.IsDark(b.def) && b.Position.InHorDistOf(center, radius)).ToList())
        {
            DarkBuildings.Smite(building);
            DarkBuildings.ToHusk(building);
        }
        // Worked out once, as the map can hold thousands of items. Another ring may have burned some of them already.
        items ??= DarkBuildings.TargetsOn(map);
        items.RemoveAll(item =>
        {
            if (!item.Spawned)
            {
                return true;
            }
            if (!item.Position.InHorDistOf(center, radius))
            {
                return false;
            }
            DarkBuildings.Burn(item);
            return true;
        });
        return radius >= MaxRadius;
    }

    public void Draw()
    {
        if (map != Find.CurrentMap || map == null || WorldRendererUtility.WorldRendered)
        {
            return;
        }
        float radius = Radius;
        float alpha = 1f - Mathf.Clamp01(radius / MaxRadius) * 0.7f;
        Vector3 pos = center.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather);
        Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, new Vector3(radius * 2f, 1f, radius * 2f));
        Graphics.DrawMesh(MeshPool.plane10, matrix, FadedMaterialPool.FadedVersionOf(RingMaterial, alpha), 0);
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref map!, "map");
        Scribe_Values.Look(ref center, "center");
        Scribe_Values.Look(ref startTick, "startTick");
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
