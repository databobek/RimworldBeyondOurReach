using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Gives a faction the ideoligion saved in Ideos/<file> (an .rid exported from the game) instead of a generated one.
// The Ideos folder sits in Mods/Ideology, so the file is only found with Ideology active.
public class DefModExtension_FixedIdeo : DefModExtension
{
    public string file = "";
}

public static class FixedIdeo
{
    public static Ideo? Load(FactionDef def, string file)
    {
        string? path = def.modContentPack?.foldersToLoadDescendingOrder
            .Select(folder => Path.Combine(folder, "Ideos", file))
            .FirstOrDefault(File.Exists);
        if (path == null)
        {
            Log.Error($"[BOR] {def.defName}: Ideos/{file} not found; generating an ideoligion instead.");
            return null;
        }
        Ideo? ideo = null;
        try
        {
            // GameDataSaveLoader.TryLoadIdeo minus the meta header, which the shipped files leave out: it would warn
            // after every game update. The flag makes the precepts take fresh IDs, as for a player-loaded ideoligion.
            GameDataSaveLoader.isSavingOrLoadingExternalIdeo = true;
            // The files reference defs from every DLC and from All Bamba Guns/Melee (relics). Without them the game
            // drops the precept or picks another relic, but logs an error for each; silence those, as vanilla does
            // for scenarios.
            using (Log.LockMessages())
            {
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref ideo, "ideo");
                Scribe.loader.FinalizeLoading();
            }
        }
        catch (Exception ex)
        {
            Scribe.ForceStop();
            Log.Error($"[BOR] {def.defName}: could not load {path}; generating an ideoligion instead. {ex}");
            ideo = null;
        }
        finally
        {
            GameDataSaveLoader.isSavingOrLoadingExternalIdeo = false;
        }
        return ideo == null ? null : IdeoGenerator.InitLoadedIdeo(ideo);
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
// Every way a faction gets its ideoligion goes through here: world generation, the ideoligion preset page and
// GameComponent_FactionIdeoRepair.
[HarmonyPatch(typeof(FactionIdeosTracker), nameof(FactionIdeosTracker.ChooseOrGenerateIdeo))]
public static class FactionIdeosTracker_ChooseOrGenerateIdeo_Patch
{
    public static bool Prefix(IdeoGenerationParms parms, Faction ___faction, ref Ideo ___primaryIdeo, List<Ideo> ___ideosMinor)
    {
        DefModExtension_FixedIdeo? ext = ___faction.def.GetModExtension<DefModExtension_FixedIdeo>();
        // Scribe is busy while a save loads (old-save conversion calls this then), so the file can't be read.
        if (ext == null || !ModsConfig.IdeologyActive || parms.forceNoExpansionIdeo || Scribe.mode != LoadSaveMode.Inactive)
        {
            return true;
        }
        Ideo? ideo = FixedIdeo.Load(___faction.def, ext.file);
        if (ideo == null)
        {
            return true;
        }
        // As vanilla's fixed ideoligions: kept out of other factions' picks, hidden if the faction asks for it.
        ideo.solid = true;
        ideo.hidden = parms.hidden;
        ideo.primaryFactionColor ??= ___faction.Color;
        ___ideosMinor.Clear();
        ___primaryIdeo = ideo;
        Find.IdeoManager.Add(ideo);
        return false;
    }
}
