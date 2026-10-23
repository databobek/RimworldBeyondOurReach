using System;
using System.Collections.Generic;
using System.Xml;
using ModSettingsFramework;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class TierSubstitution
{
    public string source = "";
    public string target = "";
}

public class TierXpathValue
{
    public string xpath = "";
    public string value = "";
}

public class IntegrationTier
{
    public string label = "";
    // Substitutions are applied in a single pass, so Steel -> Plasteel and Plasteel -> Sunsteel do not cascade.
    public List<TierSubstitution>? substitutions;
    public int researchCost;
    public string? techLevel;
    public string? prerequisite;
    public string? facility;
    public float researchViewX = -1f;
    public float researchViewY = -1f;
}

/// <summary>
/// Settings-driven integration of a third-party mod (MedPod, Replimat) into a BOR tech tier.
/// Draws a tier slider and a research cost slider in the ModSettingsFramework menu and, at patch time,
/// rewrites the cost lists, power use and research project of the configured defs. Tier 0 leaves the original mod untouched.
/// </summary>
public class PatchOperationTierIntegration : PatchOperationModSettings
{
    public string tierLabel = "Technology tier";
    public string costLabel = "Research cost";
    public string researchDef = "";
    public List<string> things = new();
    public List<IntegrationTier> tiers = new();
    // Applied for every tier but the first, which stays on the original mod's values.
    public List<TierXpathValue> powerOverrides = new();
    public int defaultTier;
    public int minCost;
    public int maxCost;
    public int costStep = 1000;
    public string researchTab = "BOR_Research_Tab";

    private int ResolveTier(ModSettingsContainer container)
    {
        int tier = Mathf.RoundToInt(container.PatchOperationValue(id, defaultTier));
        return Mathf.Clamp(tier, 0, tiers.Count - 1);
    }

    // The cost follows the tier until it is fine tuned; "<id>_costTier" remembers which tier the stored cost belongs to.
    private int ResolveCost(ModSettingsContainer container, int tier)
    {
        float syncedTier = container.PatchOperationValue(id + "_costTier", tier);
        if (Mathf.RoundToInt(syncedTier) != tier)
        {
            container.patchOperationValues[id + "_costTier"] = tier;
            container.patchOperationValues[id + "_cost"] = tiers[tier].researchCost;
        }
        float cost = container.PatchOperationValue(id + "_cost", tiers[tier].researchCost);
        return Mathf.Clamp(Mathf.RoundToInt(cost), minCost, maxCost);
    }

    public override void DoSettings(ModSettingsContainer container, Listing_Standard list)
    {
        if (tiers.NullOrEmpty())
        {
            return;
        }

        int tier = ResolveTier(container);
        tier = Mathf.RoundToInt(DrawSlider(list, $"{tierLabel}: {tiers[tier].label}", tier, tiers[tier].label, 0, tiers.Count - 1, 1f, tooltip));
        tier = Mathf.Clamp(tier, 0, tiers.Count - 1);
        container.patchOperationValues[id] = tier;

        int cost = ResolveCost(container, tier);
        cost = Mathf.RoundToInt(DrawSlider(list, $"{costLabel}: {cost:N0}", cost, cost.ToString("N0"), minCost, maxCost, costStep, null));
        cost = Mathf.Clamp(cost, minCost, maxCost);
        container.patchOperationValues[id + "_cost"] = cost;
        container.patchOperationValues[id + "_costTier"] = tier;
    }

    // PatchOperationModSettings.DoSlider cannot snap to steps, which the discrete tier slider needs.
    private float DrawSlider(Listing_Standard list, string label, float value, string valueLabel, float min, float max, float roundTo, string? explanation)
    {
        // The slider draws its value label above the bar, so the row needs headroom to keep it off the previous row.
        const float headroom = 14f;
        Rect rect = list.GetRect(Text.LineHeight + headroom);
        Rect sliderRect = rect.RightPart(.60f).Rounded();
        sliderRect.y += headroom;
        Rect labelRect = rect;
        labelRect.y += headroom - 4f;
        Widgets.Label(labelRect, label);
        scrollHeight += rect.height;
        value = Widgets.HorizontalSlider(sliderRect, value, min, max, true, valueLabel, roundTo: roundTo);
        list.Gap(5);
        scrollHeight += 5;
        if (!explanation.NullOrEmpty())
        {
            TooltipHandler.TipRegion(rect.LeftPart(0.4f), explanation);
        }
        return value;
    }

    protected override bool ApplyWorker(XmlDocument xml)
    {
        if (!CanRun() || tiers.NullOrEmpty())
        {
            return true;
        }

        var container = SettingsContainer;
        if (container == null)
        {
            return true;
        }

        int tier = ResolveTier(container);
        int cost = ResolveCost(container, tier);
        var spec = tiers[tier];

        ApplyResearch(xml, spec, cost);
        foreach (string thing in things)
        {
            var costList = xml.SelectSingleNode($"Defs/ThingDef[defName=\"{thing}\"]/costList");
            if (costList != null && !spec.substitutions.NullOrEmpty())
            {
                SubstituteCosts(xml, costList, spec.substitutions!);
            }
        }
        if (tier > 0)
        {
            foreach (var power in powerOverrides)
            {
                var node = xml.SelectSingleNode(power.xpath);
                if (node != null)
                {
                    node.InnerText = power.value;
                }
            }
        }
        return true;
    }

    private void ApplyResearch(XmlDocument xml, IntegrationTier spec, int cost)
    {
        var research = xml.SelectSingleNode($"Defs/ResearchProjectDef[defName=\"{researchDef}\"]");
        if (research == null)
        {
            return;
        }

        SetChild(xml, research, "baseCost", cost.ToString());
        if (!spec.techLevel.NullOrEmpty())
        {
            SetChild(xml, research, "techLevel", spec.techLevel!);
        }
        if (!spec.prerequisite.NullOrEmpty())
        {
            var prerequisites = GetOrCreateChild(xml, research, "prerequisites");
            prerequisites.RemoveAll();
            AppendLi(xml, prerequisites, spec.prerequisite!);
        }
        if (!spec.facility.NullOrEmpty())
        {
            var facilities = GetOrCreateChild(xml, research, "requiredResearchFacilities");
            AppendLi(xml, facilities, spec.facility!);
        }
        if (spec.researchViewX >= 0f && spec.researchViewY >= 0f)
        {
            SetChild(xml, research, "researchViewX", spec.researchViewX.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            SetChild(xml, research, "researchViewY", spec.researchViewY.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }
        if (!spec.prerequisite.NullOrEmpty())
        {
            SetChild(xml, research, "tab", researchTab);
        }
    }

    private static void SubstituteCosts(XmlDocument xml, XmlNode costList, List<TierSubstitution> substitutions)
    {
        var map = new Dictionary<string, string>();
        foreach (var substitution in substitutions)
        {
            map[substitution.source] = substitution.target;
        }

        var totals = new Dictionary<string, int>();
        var order = new List<string>();
        foreach (XmlNode child in costList.ChildNodes)
        {
            if (child.NodeType != XmlNodeType.Element || !int.TryParse(child.InnerText.Trim(), out int count))
            {
                continue;
            }
            string name = map.TryGetValue(child.Name, out var replacement) ? replacement : child.Name;
            if (!totals.ContainsKey(name))
            {
                totals[name] = 0;
                order.Add(name);
            }
            totals[name] += count;
        }

        costList.RemoveAll();
        foreach (string name in order)
        {
            var element = xml.CreateElement(name);
            element.InnerText = totals[name].ToString();
            costList.AppendChild(element);
        }
    }

    private static XmlNode GetOrCreateChild(XmlDocument xml, XmlNode parent, string name)
    {
        var child = parent.SelectSingleNode(name);
        if (child == null)
        {
            child = xml.CreateElement(name);
            parent.AppendChild(child);
        }
        return child;
    }

    private static void SetChild(XmlDocument xml, XmlNode parent, string name, string value)
    {
        GetOrCreateChild(xml, parent, name).InnerText = value;
    }

    private static void AppendLi(XmlDocument xml, XmlNode parent, string value)
    {
        foreach (XmlNode existing in parent.ChildNodes)
        {
            if (string.Equals(existing.InnerText.Trim(), value, StringComparison.Ordinal))
            {
                return;
            }
        }
        var li = xml.CreateElement("li");
        li.InnerText = value;
        parent.AppendChild(li);
    }
}
