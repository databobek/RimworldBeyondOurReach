using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Tells the player that a DefModExtension_UmbralOnly building is Umbral-only. The restriction itself is the IsForbidden
// patch (UmbralPatches.ForbidsUmbralBuilding), which gives no reason anywhere.
[StaticConstructorOnStartup]
public static class UmbralOnlyBuildings
{
    // Runs after DefInjected translations are applied, so the line is added to the translated description.
    static UmbralOnlyBuildings()
    {
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.modExtensions == null || !def.HasModExtension<DefModExtension_UmbralOnly>())
            {
                continue;
            }
            def.description += "\n\n" + "BOR_UmbralOnlyDesc".Translate();
            def.comps.Add(new CompProperties { compClass = typeof(CompUmbralOnly) });
        }
    }

    // Matches the Umbral-only half of ForbidsUmbralBuilding. After the Path of Light every dark building is forbidden to
    // all, so that case gets no Umbral-only message.
    public static bool BlocksForUmbralOnly(Thing t, Pawn pawn)
    {
        ThingDef def = t.def.entityDefToBuild as ThingDef ?? t.def;
        return def.modExtensions != null && def.HasModExtension<DefModExtension_UmbralOnly>() && !DarkBuildings.Banned(def)
            && pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike && !Umbral.IsUmbral(pawn);
    }
}

public class CompUmbralOnly : ThingComp
{
    public override string CompInspectStringExtra() => "BOR_UmbralOnlyInspect".Translate();
}

// Right-clicking an Umbral-only building, blueprint or frame with a non-Umbral pawn says why nothing can be done.
public class FloatMenuOptionProvider_UmbralOnly : FloatMenuOptionProvider
{
    protected override bool Drafted => false;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override FloatMenuOption? GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        if (!UmbralOnlyBuildings.BlocksForUmbralOnly(clickedThing, pawn))
        {
            return null;
        }
        bool plan = clickedThing.def.entityDefToBuild != null;
        string label = (clickedThing.def.entityDefToBuild ?? clickedThing.def).label;
        return new FloatMenuOption((plan ? "BOR_UmbralOnlyCannotBuild" : "BOR_UmbralOnlyCannotUse").Translate(label), null);
    }
}

// Direct orders that don't check forbiddenness themselves (flick, repair, deconstruct, research, finish frame) come out
// as disabled "X is forbidden" options. The Umbral-only option above already says why, so those are dropped.
// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
[HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), nameof(FloatMenuOptionProvider_WorkGivers.GetOptionsFor), typeof(Thing), typeof(FloatMenuContext))]
public static class FloatMenuOptionProvider_WorkGivers_GetOptionsFor_Patch
{
    public static void Postfix(Thing clickedThing, FloatMenuContext context, ref IEnumerable<FloatMenuOption> __result)
    {
        if (UmbralOnlyBuildings.BlocksForUmbralOnly(clickedThing, context.FirstSelectedPawn))
        {
            __result = __result.Where(o => !o.Disabled);
        }
    }
}
