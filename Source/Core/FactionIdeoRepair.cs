using System.Linq;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// A BOR faction must follow an ideoligion of its own culture: its leader title and ideoligion name come from it.
// The game can still hand it a foreign one, e.g. a random ideoligion when the saved one fails to load, which left
// tenebrals following Kwazaarism under a "kwazaari CEO". On load, such a faction gets a new ideoligion of its own,
// and its pawns that held the foreign one follow it.
public class GameComponent_FactionIdeoRepair : GameComponent
{
    public GameComponent_FactionIdeoRepair(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        // Classic mode gives every faction the player's ideoligion on purpose.
        if (Find.IdeoManager.classicMode)
        {
            return;
        }
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
        {
            FactionDef def = faction.def;
            Ideo? old = faction.ideos?.PrimaryIdeo;
            if (faction.IsPlayer || old == null || !def.defName.StartsWith("BOR_") || def.allowedCultures.NullOrEmpty()
                || def.allowedCultures.Contains(old.culture))
            {
                continue;
            }
            faction.ideos!.ChooseOrGenerateIdeo(new IdeoGenerationParms(def, forceNoExpansionIdeo: false, null, null,
                name: def.ideoName, styles: def.styles, deities: def.deityPresets, hidden: def.hiddenIdeo,
                description: def.ideoDescription, forcedMemes: def.forcedMemes, classicExtra: false,
                forceNoWeaponPreference: false, forNewFluidIdeo: false, fixedIdeo: def.fixedIdeo,
                requiredPreceptsOnly: def.requiredPreceptsOnly));
            Ideo ideo = faction.ideos.PrimaryIdeo;
            foreach (Pawn pawn in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where(p =>
                p.Faction == faction && p.ideo != null && p.Ideo == old && !p.IsPrisonerOfColony))
            {
                pawn.ideo.SetIdeo(ideo);
            }
            Log.Message($"[BOR] {faction.Name} followed {old.name} ({old.culture.defName}); gave it {ideo.name} instead.");
        }
    }
}
