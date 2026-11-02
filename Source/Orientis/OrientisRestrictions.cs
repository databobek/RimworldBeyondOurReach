using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Keeps the light and the dark apart: Orientis pawns leave dark things alone, and dark pawns leave Orientis things alone.
[StaticConstructorOnStartup]
public static class OrientisRestrictions
{
    private static readonly HashSet<ThingDef> DarkThings = FindDarkThings();

    private static readonly HashSet<ThingDef> OrientisThings = new(DefDatabase<ThingDef>.AllDefs
        .Where(d => d.HasModExtension<DefModExtension_OrientisBuilding>() || d.defName.StartsWith("BOR_Orientis_")));

    // Named tenebral or Umbral, or made with tenebrite: tenebrite itself, then anything that costs or is crafted from a
    // thing already in the set, until nothing new turns up.
    private static HashSet<ThingDef> FindDarkThings()
    {
        HashSet<ThingDef> dark = new() { BOR_TenebralDefOf.BOR_Tenebrite };
        dark.UnionWith(DefDatabase<ThingDef>.AllDefs.Where(d => NamedDark(d.defName) || NamedDark(d.label)));
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs.Where(d => !dark.Contains(d) && (d.CostList?.Any(c => dark.Contains(c.thingDef)) ?? false)).ToList())
            {
                grew |= dark.Add(def);
            }
            foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefs.Where(r => r.products != null && r.ingredients.Any(i => i.IsFixedIngredient && dark.Contains(i.FixedIngredient))))
            {
                foreach (ThingDefCountClass product in recipe.products)
                {
                    grew |= dark.Add(product.thingDef);
                }
            }
        }
        return dark;
    }

    private static bool NamedDark(string? name) =>
        name != null && (name.IndexOf("tenebr", System.StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("umbral", System.StringComparison.OrdinalIgnoreCase) >= 0);

    public static bool IsDarkThing(Thing thing)
    {
        Thing inner = thing.GetInnerIfMinified();
        return DarkThings.Contains(thing.def.entityDefToBuild as ThingDef ?? inner.def) || (inner.Stuff != null && DarkThings.Contains(inner.Stuff));
    }

    private static bool IsOrientisThing(Thing thing) => OrientisThings.Contains(thing.def.entityDefToBuild as ThingDef ?? thing.GetInnerIfMinified().def);

    // The set lookup comes first: this runs on every forbid check, and most things are in neither set.
    public static bool Blocks(Pawn pawn, Thing thing, out string reason)
    {
        if (IsDarkThing(thing) && Orientis.IsOrientis(pawn))
        {
            reason = "BOR_OrientisBlocksDark".Translate();
            return true;
        }
        if (IsOrientisThing(thing) && CompMeditationShield.IsDark(pawn))
        {
            reason = "BOR_DarkBlocksOrientis".Translate();
            return true;
        }
        reason = "";
        return false;
    }

    // Everything dark a pawn wears, wields, carries or has in its inventory. An Umbral's grown exoskeleton and pod are part
    // of its body, so they go with the pawn and not here.
    private static List<Thing> DarkGearOf(Pawn pawn)
    {
        List<Thing> gear = new();
        if (pawn.apparel != null)
        {
            gear.AddRange(pawn.apparel.WornApparel.Where(a => IsDarkThing(a) && !Umbral.IsExoApparel(a.def)));
        }
        if (pawn.equipment != null)
        {
            gear.AddRange(pawn.equipment.AllEquipmentListForReading.Where(e => IsDarkThing(e) && !Umbral.IsUtilityWeapon(e.def)));
        }
        if (pawn.inventory != null)
        {
            gear.AddRange(pawn.inventory.innerContainer.Where(IsDarkThing));
        }
        Thing? carried = pawn.carryTracker?.CarriedThing;
        if (carried != null && IsDarkThing(carried))
        {
            gear.Add(carried);
        }
        return gear;
    }

    public static bool HasDarkGear(Pawn pawn) => DarkGearOf(pawn).Count > 0;

    // Burns it all off the pawn, wherever the pawn is, as the Path of Light strips the tenebral parts.
    public static void BurnDarkGear(Pawn pawn)
    {
        List<Thing> gear = DarkGearOf(pawn);
        foreach (Thing thing in gear)
        {
            if (thing is Apparel apparel && pawn.apparel != null && pawn.apparel.WornApparel.Contains(apparel))
            {
                pawn.apparel.Remove(apparel);
                apparel.Destroy();
            }
            else if (thing is ThingWithComps equipment && pawn.equipment != null && pawn.equipment.AllEquipmentListForReading.Contains(equipment))
            {
                pawn.equipment.DestroyEquipment(equipment);
            }
            else
            {
                thing.Destroy();
            }
        }
        if (gear.Count > 0 && pawn.Spawned)
        {
            DarkBuildings.Smite(pawn);
        }
    }

    // For a pawn made Orientis outside the sequence. Off the map it keeps the gear in its inventory, unused.
    public static void DropDarkGear(Pawn pawn)
    {
        foreach (Apparel apparel in pawn.apparel?.WornApparel.Where(IsDarkThing).ToList() ?? new List<Apparel>())
        {
            if (pawn.Spawned)
            {
                pawn.apparel!.TryDrop(apparel, out _, pawn.PositionHeld, forbid: false);
            }
            else
            {
                pawn.apparel!.Remove(apparel);
                pawn.inventory?.innerContainer.TryAdd(apparel);
            }
        }
        foreach (ThingWithComps weapon in pawn.equipment?.AllEquipmentListForReading.Where(IsDarkThing).ToList() ?? new List<ThingWithComps>())
        {
            if (pawn.Spawned)
            {
                pawn.equipment!.TryDropEquipment(weapon, out _, pawn.PositionHeld, forbid: false);
            }
            else
            {
                pawn.equipment!.Remove(weapon);
                pawn.inventory?.innerContainer.TryAdd(weapon);
            }
        }
        if (pawn.Spawned && pawn.inventory != null)
        {
            foreach (Thing thing in pawn.inventory.innerContainer.Where(IsDarkThing).ToList())
            {
                pawn.inventory.innerContainer.TryDrop(thing, pawn.PositionHeld, pawn.Map, ThingPlaceMode.Near, out _);
            }
        }
    }

    [HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.IsForbidden), typeof(Thing), typeof(Pawn))]
    public static class ForbidUtility_IsForbidden_Patch
    {
        // A building marked for deconstruction can still be taken down.
        public static void Postfix(Thing t, Pawn pawn, ref bool __result)
        {
            if (!__result && pawn != null && pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike
                && Blocks(pawn, t, out _) && t.MapHeld?.designationManager.DesignationOn(t) == null)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip),
        new[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) },
        new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
    public static class EquipmentUtility_CanEquip_Patch
    {
        public static void Postfix(Thing thing, Pawn pawn, ref string cantReason, ref bool __result)
        {
            if (__result && Blocks(pawn, thing, out string reason))
            {
                cantReason = reason;
                __result = false;
            }
        }
    }
}
