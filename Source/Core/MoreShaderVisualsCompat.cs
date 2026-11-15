using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// How a shield looks when More Shader Visuals draws it. Read only by MoreShaderVisualsCompat.
public class ShieldVisualsExtension : DefModExtension
{
    // Personal shields only. MSV tints domes with the interceptor's own color already.
    public Color color = Color.white;

    // One of MSV's dome styles: Crisp, Hex, Vanilla, Bubble, HexBubble. Empty keeps the player's choice.
    public string? domeStyle;
}

// Draws our shields through More Shader Visuals in our own colors and styles. Their code is never referenced, only
// patched by name. If a name changes in an update, the compat logs once and their normal drawing stays.
//
// MSV keeps the dome style on one shared material, so forcing a style per shield needs a material of our own:
// while one of our shields draws, their style setting is swapped to ours and their material lookup hands back
// one of our materials, which their own code then sets up for that style. Both are put back after the draw.
[StaticConstructorOnStartup]
public static class MoreShaderVisualsCompat
{
    private const string PackageId = "assssssqwww.visualtweaks";
    private const string Ns = "VisualTweaks.";

    // A dome that was up within this many frames counts as just seen, so one scrolled back to later does not collapse then.
    private const int SeenFrames = 2;

    private static FieldInfo? largeStyleField;
    private static FieldInfo? personalStyleField;
    private static MethodInfo? ensureMethod;
    private static MethodInfo? applyLookMethod;
    private static MethodInfo? dissolveBegin;
    private static MethodInfo? dissolveProgress;
    private static PropertyInfo? largeBreakStyle;
    private static ConstructorInfo? materialCtor;
    private static object? largeTarget;
    private static object? personalTarget;

    private static readonly Dictionary<ThingDef, object> StyleByDef = new();
    private static readonly Dictionary<(bool personal, object style), object> Materials = new();
    private static readonly ConditionalWeakTable<ThingComp, StrongBox<(bool present, int frame)>> LastSeen = new();

    // Set only while one of our shields draws.
    private static object? redirect;
    private static FieldInfo? swappedField;
    private static object? savedStyle;

    static MoreShaderVisualsCompat()
    {
        if (!ModsConfig.IsActive(PackageId))
        {
            return;
        }
        try
        {
            Setup();
        }
        catch (Exception e)
        {
            Log.Error($"[BOR] More Shader Visuals compat failed; shields keep its normal drawing. {e}");
        }
    }

    private static void Setup()
    {
        Type? settings = AccessTools.TypeByName(Ns + "VisualTweaksSettings");
        Type? domeRenderer = AccessTools.TypeByName(Ns + "ShieldDomeRenderer");
        Type? personalRenderer = AccessTools.TypeByName(Ns + "PersonalShieldRenderer");
        Type? material = AccessTools.TypeByName(Ns + "ShieldDomeMaterial");
        Type? target = AccessTools.TypeByName(Ns + "ShieldHexTarget");
        Type? dissolve = AccessTools.TypeByName(Ns + "ShieldDissolveLog");
        largeStyleField = settings == null ? null : AccessTools.Field(settings, "shieldDomeStyle");
        personalStyleField = settings == null ? null : AccessTools.Field(settings, "personalShieldDomeStyle");
        largeBreakStyle = settings == null ? null : AccessTools.Property(settings, "CurrentShieldBreakStyle");
        ensureMethod = material == null ? null : AccessTools.Method(material, "Ensure");
        applyLookMethod = material == null ? null : AccessTools.Method(material, "ApplyLook");
        materialCtor = material == null || target == null ? null : AccessTools.Constructor(material, new[] { target });
        dissolveBegin = dissolve == null ? null : AccessTools.Method(dissolve, "Begin");
        dissolveProgress = dissolve == null ? null : AccessTools.Method(dissolve, "Progress");
        MethodInfo? largeDraw = domeRenderer == null ? null : AccessTools.Method(domeRenderer, "DrawCore");
        MethodInfo? personalDraw = personalRenderer == null ? null : AccessTools.Method(personalRenderer, "DrawCore");

        if (largeStyleField == null || personalStyleField == null || largeBreakStyle == null || ensureMethod == null
            || applyLookMethod == null || materialCtor == null || target == null || dissolveBegin == null
            || dissolveProgress == null || largeDraw == null || personalDraw == null)
        {
            Log.Warning("[BOR] More Shader Visuals has changed; our shields keep its normal drawing.");
            return;
        }
        largeTarget = Enum.Parse(target, "Large");
        personalTarget = Enum.Parse(target, "Personal");
        ReadStyles(largeStyleField.FieldType);

        Harmony harmony = new("bambaryla.bor.redux.moreshadervisuals");
        HarmonyMethod finalizer = new(AccessTools.Method(typeof(MoreShaderVisualsCompat), nameof(DrawFinalizer)));
        harmony.Patch(largeDraw, prefix: Method(nameof(LargeDrawPrefix)), finalizer: finalizer);
        harmony.Patch(personalDraw, prefix: Method(nameof(PersonalDrawPrefix)), finalizer: finalizer);
        harmony.Patch(ensureMethod, prefix: Method(nameof(EnsurePrefix)));
        harmony.Patch(applyLookMethod, prefix: Method(nameof(ApplyLookPrefix)));
    }

    private static HarmonyMethod Method(string name) => new(AccessTools.Method(typeof(MoreShaderVisualsCompat), name));

    private static void ReadStyles(Type styleType)
    {
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            string? name = def.GetModExtension<ShieldVisualsExtension>()?.domeStyle;
            if (name.NullOrEmpty())
            {
                continue;
            }
            if (Enum.IsDefined(styleType, name!))
            {
                StyleByDef[def] = Enum.Parse(styleType, name!);
            }
            else
            {
                Log.Error($"[BOR] {def.defName} asks for shield style '{name}', which More Shader Visuals does not have.");
            }
        }
    }

    public static bool LargeDrawPrefix(ThingComp comp, bool present, ref bool __result)
    {
        ThingDef? def = comp?.parent?.def;
        if (def == null)
        {
            return true;
        }
        // Report it drawn, so neither MSV nor vanilla draws the hidden dome.
        if (comp!.parent.TryGetComp<CompUmbralShield>() is { DomeHidden: true })
        {
            __result = true;
            return false;
        }
        if (StyleByDef.TryGetValue(def, out var style))
        {
            BeginOverride(largeStyleField!, style, personal: false);
        }
        if (def.HasModExtension<ShieldVisualsExtension>())
        {
            CollapseWhenLowered(comp, present);
        }
        return true;
    }

    public static void PersonalDrawPrefix(object comp, ref Color baseColor)
    {
        ThingDef? def = (comp as ThingComp)?.parent?.def;
        ShieldVisualsExtension? ext = def?.GetModExtension<ShieldVisualsExtension>();
        if (ext == null)
        {
            return;
        }
        baseColor = ext.color;
        if (StyleByDef.TryGetValue(def!, out var style))
        {
            BeginOverride(personalStyleField!, style, personal: true);
        }
    }

    public static void DrawFinalizer()
    {
        if (swappedField != null)
        {
            swappedField.SetValue(null, savedStyle);
            swappedField = null;
        }
        redirect = null;
    }

    private static void BeginOverride(FieldInfo field, object style, bool personal)
    {
        if (!Materials.TryGetValue((personal, style), out var material))
        {
            material = materialCtor!.Invoke(new[] { personal ? personalTarget : largeTarget });
            Materials[(personal, style)] = material;
        }
        savedStyle = field.GetValue(null);
        swappedField = field;
        field.SetValue(null, style);
        redirect = material;
    }

    // Calling the method on our material runs this prefix again, where the instance is ours and it passes through.
    public static bool EnsurePrefix(object __instance, ref Material? __result)
    {
        if (redirect == null || __instance == redirect)
        {
            return true;
        }
        __result = (Material?)ensureMethod!.Invoke(redirect, null);
        return false;
    }

    public static bool ApplyLookPrefix(object __instance)
    {
        if (redirect == null || __instance == redirect)
        {
            return true;
        }
        applyLookMethod!.Invoke(redirect, null);
        return false;
    }

    // MSV dissolves a dome only when it breaks. Ours also drop when lowered or out of power, so they dissolve then too.
    private static void CollapseWhenLowered(ThingComp comp, bool present)
    {
        int frame = Time.frameCount;
        StrongBox<(bool present, int frame)> seen = LastSeen.GetOrCreateValue(comp);
        bool dropped = seen.Value.present && !present && frame - seen.Value.frame <= SeenFrames;
        seen.Value = (present, frame);
        if (!dropped || largeBreakStyle!.GetValue(null)?.ToString() == "Vanilla")
        {
            return;
        }
        object?[] args = { comp, 0f };
        if (dissolveProgress!.Invoke(null, args) == null)
        {
            dissolveBegin!.Invoke(null, new object[] { comp });
        }
    }
}
