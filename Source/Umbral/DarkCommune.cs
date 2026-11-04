using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

// Tier V research cannot be finished at the bench; the umbral spire offers a deadly ritual per building instead.
public class CompProperties_DarkCommune : CompProperties
{
    public List<ResearchProjectDef> researches = new();

    public CompProperties_DarkCommune()
    {
        compClass = typeof(CompDarkCommune);
    }
}

public class CompDarkCommune : ThingComp
{
    private CompProperties_DarkCommune Props => (CompProperties_DarkCommune)props;

    private static readonly Dictionary<ResearchProjectDef, ThingDef?> buildings = new();

    // The tier V building the research unlocks, shown as the gizmo. Gizmos rebuild every frame, so the search is kept.
    private static ThingDef? BuildingFor(ResearchProjectDef research)
    {
        if (!buildings.TryGetValue(research, out ThingDef? building))
        {
            building = DefDatabase<ThingDef>.AllDefs.FirstOrDefault(d => d.building != null && d.researchPrerequisites != null && d.researchPrerequisites.Contains(research));
            buildings[research] = building;
        }
        return building;
    }

    private static bool Communing(Pawn pawn) => pawn.health.hediffSet.hediffs.Any(h => h is Hediff_DarkCommune);

    private bool InProgress(ResearchProjectDef research) =>
        parent.Map.mapPawns.AllPawns.Any(p => p.health?.hediffSet?.hediffs.Any(h => h is Hediff_DarkCommune c && c.research == research) ?? false);

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        foreach (ResearchProjectDef research in Props.researches)
        {
            ThingDef? building = BuildingFor(research);
            if (research.IsFinished || building == null)
            {
                continue;
            }
            ResearchProjectDef r = research;
            Command_Action command = new()
            {
                defaultLabel = building.LabelCap,
                defaultDesc = "BOR_CommuneDesc".Translate(building.label),
                icon = building.uiIcon,
                action = () => ChoosePawn(r, building)
            };
            if (!r.PrerequisitesCompleted)
            {
                command.Disable("BOR_CommuneNeedsResearch".Translate(r.prerequisites.Where(p => !p.IsFinished).Select(p => p.LabelCap.Resolve()).ToCommaList()));
            }
            else if (InProgress(r))
            {
                command.Disable("BOR_CommuneInProgress".Translate());
            }
            yield return command;
        }
    }

    private void ChoosePawn(ResearchProjectDef research, ThingDef building)
    {
        List<FloatMenuOption> options = new();
        foreach (Pawn pawn in parent.Map.mapPawns.FreeColonistsSpawned.Where(Umbral.IsUmbral))
        {
            Pawn p = pawn;
            if (Communing(p))
            {
                options.Add(new FloatMenuOption(p.LabelShortCap + " (" + "BOR_CommunePawnBusy".Translate() + ")", null));
                continue;
            }
            options.Add(new FloatMenuOption(p.LabelShortCap, () =>
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("BOR_CommuneConfirm".Translate(p.LabelShortCap, building.label), () => Begin(p, research), true))));
        }
        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("BOR_CommuneNoUmbral".Translate(), null));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void Begin(Pawn pawn, ResearchProjectDef research)
    {
        if (pawn.Dead || !pawn.Spawned || Communing(pawn) || InProgress(research))
        {
            return;
        }
        Map map = pawn.Map;
        map.weatherManager.eventHandler.AddEvent(new WeatherEvent_RedLightning(map, pawn.Position));
        Hediff_DarkCommune hediff = (Hediff_DarkCommune)HediffMaker.MakeHediff(BOR_UmbralDefOf.BOR_DarkArchotechCommune, pawn);
        hediff.research = research;
        pawn.health.AddHediff(hediff);
    }
}

// A disease: severity climbs until the pawn's immunity catches up. The research unlocks when the illness ends,
// whether the pawn builds full immunity or dies of it.
public class Hediff_DarkCommune : HediffWithComps
{
    public ResearchProjectDef? research;
    private bool unlocked;

    private void Unlock()
    {
        if (unlocked || research == null)
        {
            return;
        }
        unlocked = true;
        Find.ResearchManager.FinishProject(research, true, pawn);
    }

    public override void Tick()
    {
        base.Tick();
        if (!unlocked && !pawn.Dead && (this.TryGetComp<HediffComp_Immunizable>()?.FullyImmune ?? false))
        {
            Unlock();
        }
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null!)
    {
        base.Notify_PawnDied(dinfo, culprit);
        Unlock();
    }

    // The illness only ends legitimately by full immunity (which already unlocked) or death. Anything else that
    // removes it while the pawn lives (a med pod, a healer serum, dev tools) cancels the commune: no research.
    public override void PostRemoved()
    {
        base.PostRemoved();
        if (unlocked || research == null || pawn.Dead || pawn.Destroyed)
        {
            return;
        }
        if (this.TryGetComp<HediffComp_Immunizable>()?.FullyImmune ?? false)
        {
            Unlock();
            return;
        }
        Find.LetterStack.ReceiveLetter("BOR_CommuneCuredLabel".Translate(pawn.LabelShortCap), "BOR_CommuneCured".Translate(pawn.LabelShortCap, research.LabelCap),
            LetterDefOf.NegativeEvent, pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref research, "communeResearch");
        Scribe_Values.Look(ref unlocked, "communeUnlocked");
    }
}

// Vanilla lightning strike, drawn with a red bolt. Same flame burst, smoke and thunder.
[StaticConstructorOnStartup]
public class WeatherEvent_RedLightning : WeatherEvent
{
    private const int Duration = 15;

    private static readonly Material BoltMaterial = MaterialPool.MatFrom("Weather/LightningBolt", ShaderDatabase.Transparent, new Color(1f, 0.1f, 0.1f));

    private readonly IntVec3 strikeLoc;
    private Mesh? boltMesh;
    private int age;

    public WeatherEvent_RedLightning(Map map, IntVec3 strikeLoc) : base(map)
    {
        this.strikeLoc = strikeLoc;
    }

    public override bool Expired => age > Duration;

    public override void FireEvent()
    {
        boltMesh = LightningBoltMeshPool.RandomBoltMesh;
        if (!strikeLoc.Fogged(map))
        {
            GenExplosion.DoExplosion(strikeLoc, map, 1.9f, DamageDefOf.Flame, null);
            Vector3 loc = strikeLoc.ToVector3Shifted();
            for (int i = 0; i < 4; i++)
            {
                FleckMaker.ThrowSmoke(loc, map, 1.5f);
                FleckMaker.ThrowMicroSparks(loc, map);
                FleckMaker.ThrowLightningGlow(loc, map, 1.5f);
            }
        }
        SoundDefOf.Thunder_OnMap.PlayOneShot(SoundInfo.InMap(new TargetInfo(strikeLoc, map)));
    }

    public override void WeatherEventTick()
    {
        age++;
    }

    public override void WeatherEventDraw()
    {
        if (boltMesh != null)
        {
            Graphics.DrawMesh(boltMesh, strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), Quaternion.identity,
                FadedMaterialPool.FadedVersionOf(BoltMaterial, 1f - age / (float)Duration), 0);
        }
    }
}
