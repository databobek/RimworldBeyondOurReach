using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Marks a tenebral building. Umbral buildings are recognized by DefModExtension_UmbralOnly.
public class DefModExtension_TenebralBuilding : DefModExtension
{
}

// What the Path of Light does to the colony's tenebral and Umbral buildings.
public static class DarkBuildings
{
    public static bool IsDark(BuildableDef? def) =>
        def?.modExtensions != null && (def.HasModExtension<DefModExtension_TenebralBuilding>() || def.HasModExtension<DefModExtension_UmbralOnly>());

    public static bool Banned(BuildableDef? def) => IsDark(def) && GameComponent_OrientisPath.Instance.Path == ColonyPath.Light;

    private static readonly Color SmiteColor = new(1f, 0.8f, 0.25f, 1f);

    // The gold flash when the light reaches something dark.
    public static void Smite(Thing thing)
    {
        Vector3 pos = thing.TrueCenter();
        FleckCreationData flash = FleckMaker.GetDataStatic(pos, thing.Map, FleckDefOf.LightningGlow, 2f + thing.def.size.Magnitude);
        flash.instanceColor = SmiteColor;
        thing.Map.flecks.CreateFleck(flash);
        FleckMaker.ThrowMicroSparks(pos, thing.Map);
    }

    // Dark items lying on the map, wherever they are stored. Things worn, carried or held in a container are out of reach.
    public static List<Thing> ItemsOn(Map map) => map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver)
        .Where(t => t.def.category == ThingCategory.Item && OrientisRestrictions.IsDarkThing(t)).ToList();

    public static void Burn(Thing item)
    {
        Smite(item);
        item.Destroy();
    }

    // Burns what no ring reached, such as gear dropped during conversion and items on maps with no altar.
    public static void BurnAllItems()
    {
        foreach (Map map in Find.Maps)
        {
            ItemsOn(map).ForEach(Burn);
        }
    }

    // Plans are cancelled with a refund, and standing buildings die where they stand.
    public static void ShutDown()
    {
        foreach (Map map in Find.Maps)
        {
            foreach (Thing plan in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).Concat(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingFrame)).ToList())
            {
                if (plan.Faction == Faction.OfPlayer && IsDark(plan.def.entityDefToBuild))
                {
                    plan.Destroy(DestroyMode.Cancel);
                }
            }
            foreach (Building building in map.listerBuildings.allBuildingsColonist.Where(b => IsDark(b.def)).ToList())
            {
                ToHusk(building);
            }
        }
    }

    public static void ToHusk(Building building)
    {
        ThingDef def = building.def;
        IntVec3 cell = building.Position;
        Rot4 rot = building.Rotation;
        Map map = building.Map;
        if (building is Building_Casket casket)
        {
            casket.EjectContents();
        }
        building.Destroy();
        ThingDef? huskDef = DefDatabase<ThingDef>.GetNamedSilentFail("BOR_DarkHusk_" + def.size.x + "x" + def.size.z + "_Shell");
        if (huskDef == null)
        {
            Log.Warning("[BOR] No husk of size " + def.size + " for " + def.defName + ".");
            return;
        }
        Building_DarkHusk husk = (Building_DarkHusk)ThingMaker.MakeThing(huskDef);
        husk.original = def;
        husk.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(husk, cell, map, rot, WipeMode.VanishOrMoveAside);
    }
}

// A dead building: looks like the one it was, greyed out, and does nothing. There is one def per footprint.
public class Building_DarkHusk : Building
{
    private static readonly Color DeadColor = new(0.35f, 0.35f, 0.35f);

    public ThingDef? original;

    private Graphic? deadGraphic;

    public override Graphic Graphic =>
        original == null ? base.Graphic : deadGraphic ??= original.graphic.GetColoredVersion(original.graphic.Shader, DeadColor, DeadColor);

    public override string LabelNoCount => original == null ? base.LabelNoCount : "BOR_DarkHusk".Translate(original.label);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref original, "original");
    }
}
