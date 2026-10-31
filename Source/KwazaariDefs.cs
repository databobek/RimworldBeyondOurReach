using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Kwazaari rework: hidden faction reached through the comm console; see openspec/specs/kwazaari-rework
[DefOf]
public static class BOR_KwazaariDefOf
{
    public static FactionDef BOR_KwazaarBazaar = null!;
    public static JobDef BOR_ConnectKwazaari = null!;
    public static JobDef BOR_BoardKwazaariShuttle = null!;
    public static ThingDef BOR_KwazaariShuttle = null!;
    public static ThingDef BOR_KwazaariShuttleIncoming = null!;
    public static ThingDef BOR_KwazaariShuttleLeaving = null!;
    public static HediffDef BOR_KwazaariLoan = null!;

    static BOR_KwazaariDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_KwazaariDefOf));
    }
}

[StaticConstructorOnStartup]
public static class KwazaariTex
{
    public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("BOR/Things/Factions/KwazaariIcon");
    public static readonly Texture2D Scrip = ContentFinder<Texture2D>.Get("BOR/Things/Factions/Favor");
}

// Things the Kwazaari refuse to buy. Every instance is merged, so other files and patches can add entries.
public class KwazaariTradeBlacklistDef : Def
{
    public List<ThingDef> thingDefs = new();
    public List<ThingCategoryDef> thingCategories = new();

    private static HashSet<ThingDef>? blocked;

    public static bool Blocks(ThingDef def)
    {
        if (blocked == null)
        {
            blocked = new HashSet<ThingDef>();
            foreach (KwazaariTradeBlacklistDef list in DefDatabase<KwazaariTradeBlacklistDef>.AllDefs)
            {
                blocked.AddRange(list.thingDefs);
                foreach (ThingCategoryDef cat in list.thingCategories)
                {
                    blocked.AddRange(cat.DescendantThingDefs);
                }
            }
        }
        return blocked.Contains(def);
    }
}

// One entry in the spend-scrip window. The worker decides what is delivered; the fields below are its settings.
public class KwazaariFavorDef : Def
{
    public Type workerClass = typeof(FavorWorker_Drop);
    public float cost;
    public float cooldownDays;
    public int order;
    public string? iconPath;
    // Package IDs of DLCs or mods the favor needs; any one of them is enough, e.g. ludeon.rimworld.royalty.
    public List<string> requiredAnyOf = new();

    public List<ThingDefCountClass> things = new();
    // Count for the thing the player picks (skill trainers, stone blocks).
    public int chosenCount;

    public PawnKindDef? pawnKind;
    public int pawnCount;
    public float loanHours;
    public float lostPawnCost;

    [Unsaved] private FavorWorker? worker;
    [Unsaved] private Texture2D? icon;

    public FavorWorker Worker
    {
        get
        {
            if (worker == null)
            {
                worker = (FavorWorker)Activator.CreateInstance(workerClass)!;
                worker.def = this;
            }
            return worker;
        }
    }

    public Texture2D Icon
    {
        get
        {
            if (icon == null)
            {
                icon = iconPath.NullOrEmpty() ? things.FirstOrDefault()?.thingDef?.uiIcon ?? KwazaariTex.Icon
                    : ContentFinder<Texture2D>.Get(iconPath);
            }
            return icon;
        }
    }

    public bool PackageActive => requiredAnyOf.Count == 0 || requiredAnyOf.Any(ModsConfig.IsActive);

    public int CooldownTicks => Mathf.RoundToInt(cooldownDays * GenDate.TicksPerDay);

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string e in base.ConfigErrors())
        {
            yield return e;
        }
        if (workerClass == null || !typeof(FavorWorker).IsAssignableFrom(workerClass))
        {
            yield return "workerClass must derive from BeyondOurReach.FavorWorker";
        }
    }
}
