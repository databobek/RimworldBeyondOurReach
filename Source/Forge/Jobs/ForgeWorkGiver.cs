using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// The forge work givers only differ in which forges have their kind of work and in the job they hand out.
public abstract class WorkGiver_ForgeBase : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

    // Only asked about forges the player owns.
    protected abstract bool HasWork(CompForge comp);

    private bool HasPlayerWork(CompForge comp) => comp.parent.Faction == Faction.OfPlayer && HasWork(comp);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (CompForge comp in MapComponent_WorkBuildings.Of(pawn.Map).forges)
        {
            if (HasPlayerWork(comp))
            {
                yield return comp.parent;
            }
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        foreach (CompForge comp in MapComponent_WorkBuildings.Of(pawn.Map).forges)
        {
            if (HasPlayerWork(comp))
            {
                return false;
            }
        }
        return true;
    }
}
