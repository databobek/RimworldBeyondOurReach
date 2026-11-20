using Verse;

namespace BeyondOurReach;

// The Coventech mechsuit framework builds each mech's blueprint def in code as "<pawn kind>_Building" and copies the
// pawn kind's body graphic, but not the race's uiIconScale. Mechs drawn large (the Helios at size 4) then get an
// oversized build-menu icon, so the race's scale is copied over here. Looked up by name: no reference to the framework.
[StaticConstructorOnStartup]
public static class MechBlueprintIcons
{
    static MechBlueprintIcons()
    {
        foreach (PawnKindDef kind in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            ThingDef? race = kind.race;
            if (race == null || race.uiIconScale == 1f)
            {
                continue;
            }
            ThingDef? blueprint = DefDatabase<ThingDef>.GetNamedSilentFail(kind.defName + "_Building");
            if (blueprint != null)
            {
                blueprint.uiIconScale = race.uiIconScale;
            }
        }
    }
}
