using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

// Marks a building as placed by the manifest ability. These have no research and no Architect entry.
public class DefModExtension_OrientisBuilding : DefModExtension
{
}

public static class OrientisManifest
{
    private const float BigSkipScale = 3f;

    private static List<ThingDef>? buildings;

    public static List<ThingDef> Buildings => buildings ??= DefDatabase<ThingDef>.AllDefs
        .Where(d => d.HasModExtension<DefModExtension_OrientisBuilding>())
        .OrderBy(d => d.label)
        .ToList();

    private static bool Usable(Thing thing) =>
        thing.Spawned && !thing.IsForbidden(Faction.OfPlayer) && !thing.Position.Fogged(thing.Map);

    public static int Available(Map map, ThingDef material) =>
        map.listerThings.ThingsOfDef(material).Where(Usable).Sum(t => t.stackCount);

    // God mode places for free.
    public static bool CanPay(Map map, ThingDef building) =>
        DebugSettings.godMode || building.CostList.NullOrEmpty() || building.CostList.All(cost => Available(map, cost.thingDef) >= cost.count);

    // Nearest stacks go first; each stack it takes from disappears in a skip.
    private static void TakeCost(Map map, ThingDef building, IntVec3 cell)
    {
        if (DebugSettings.godMode || building.CostList.NullOrEmpty())
        {
            return;
        }
        foreach (ThingDefCountClass cost in building.CostList)
        {
            int remaining = cost.count;
            foreach (Thing stack in map.listerThings.ThingsOfDef(cost.thingDef).Where(Usable).OrderBy(t => t.Position.DistanceToSquared(cell)).ToList())
            {
                if (remaining <= 0)
                {
                    break;
                }
                int taken = Mathf.Min(remaining, stack.stackCount);
                KwazaariUtility.PlaySkip(stack.Position, map);
                stack.SplitOff(taken).Destroy();
                remaining -= taken;
            }
        }
    }

    public static void Place(Map map, ThingDef building, IntVec3 cell, Rot4 rot)
    {
        TakeCost(map, building, cell);
        Thing thing = ThingMaker.MakeThing(building);
        thing.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(thing, cell, map, rot, WipeMode.VanishOrMoveAside);
        KwazaariUtility.PlaySkip(thing.TrueCenter().ToIntVec3(), map, BigSkipScale);
    }
}

// The ability never targets anything: clicking it opens the list of buildings.
public class Command_OrientisManifest : Command_Ability
{
    public Command_OrientisManifest(Ability ability, Pawn pawn) : base(ability, pawn)
    {
    }

    public override void ProcessInput(Event ev)
    {
        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        Find.WindowStack.Add(new Window_OrientisManifest(Pawn.Map));
    }
}

public class Window_OrientisManifest : Window
{
    private const float RowHeight = 124f;
    private const float Gap = 10f;
    private const float CostIconSize = 24f;
    private const float ButtonWidth = 120f;

    private readonly Map map;
    private Vector2 scroll;

    public override Vector2 InitialSize => new(760f, 680f);

    public Window_OrientisManifest(Map map)
    {
        this.map = map;
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = false;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 36f), "BOR_ManifestTitle".Translate());
        Text.Font = GameFont.Small;
        List<ThingDef> buildings = OrientisManifest.Buildings;
        Rect outRect = new(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
        Rect viewRect = new(0f, 0f, outRect.width - 16f, buildings.Count * (RowHeight + Gap));
        Widgets.BeginScrollView(outRect, ref scroll, viewRect);
        float y = 0f;
        foreach (ThingDef building in buildings)
        {
            DrawRow(new Rect(0f, y, viewRect.width, RowHeight), building);
            y += RowHeight + Gap;
        }
        Widgets.EndScrollView();
    }

    private void DrawRow(Rect rect, ThingDef building)
    {
        Widgets.DrawMenuSection(rect);
        Rect inner = rect.ContractedBy(Gap);
        Widgets.ThingIcon(new Rect(inner.x, inner.y, inner.height, inner.height), building);
        float textX = inner.x + inner.height + Gap;
        float textWidth = inner.xMax - textX - ButtonWidth - Gap;
        Widgets.Label(new Rect(textX, inner.y, textWidth, 24f), building.LabelCap);
        Text.Font = GameFont.Tiny;
        Widgets.Label(new Rect(textX, inner.y + 24f, textWidth, inner.height - 24f - CostIconSize), building.description);
        Text.Font = GameFont.Small;
        DrawCost(new Rect(textX, inner.yMax - CostIconSize, textWidth, CostIconSize), building);
        Rect button = new(inner.xMax - ButtonWidth, inner.y + (inner.height - 36f) / 2f, ButtonWidth, 36f);
        if (Widgets.ButtonText(button, "BOR_ManifestPlace".Translate(), active: OrientisManifest.CanPay(map, building)))
        {
            Close();
            Find.DesignatorManager.Select(new Designator_OrientisBuilding(building));
        }
    }

    private void DrawCost(Rect rect, ThingDef building)
    {
        if (building.CostList.NullOrEmpty())
        {
            return;
        }
        float x = rect.x;
        Text.Anchor = TextAnchor.MiddleLeft;
        foreach (ThingDefCountClass cost in building.CostList)
        {
            int available = OrientisManifest.Available(map, cost.thingDef);
            Rect icon = new(x, rect.y, CostIconSize, CostIconSize);
            Widgets.ThingIcon(icon, cost.thingDef);
            TooltipHandler.TipRegion(icon, cost.thingDef.LabelCap);
            string amount = available + "/" + cost.count;
            float width = Text.CalcSize(amount).x + Gap;
            GUI.color = available >= cost.count ? Color.white : ColorLibrary.RedReadable;
            Widgets.Label(new Rect(icon.xMax + 2f, rect.y, width, CostIconSize), amount);
            GUI.color = Color.white;
            x = icon.xMax + 2f + width;
        }
        Text.Anchor = TextAnchor.UpperLeft;
    }
}

// Normal building placement, except that confirming a spot pays and finishes the building at once.
public class Designator_OrientisBuilding : Designator_Place
{
    private readonly ThingDef building;

    public Designator_OrientisBuilding(ThingDef building)
    {
        this.building = building;
        icon = building.uiIcon;
        iconAngle = building.uiIconAngle;
        defaultLabel = building.LabelCap;
        defaultDesc = building.description;
        soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
    }

    public override BuildableDef PlacingDef => building;

    public override ThingStyleDef? ThingStyleDefForPreview => null;

    public override ThingDef? StuffDef => null;

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        if (!OrientisManifest.CanPay(Map, building))
        {
            return "BOR_ManifestCannotPay".Translate(building.label);
        }
        return GenConstruct.CanPlaceBlueprintAt(building, c, placingRot, Map);
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        OrientisManifest.Place(Map, building, c, placingRot);
        Find.DesignatorManager.Deselect();
    }
}
