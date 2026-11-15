using System.Globalization;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Gives a building's inspector tabs (BOR's own, the resource drill tab and the bills tab) a BorUISchemeDef.
public class BorUIScheme : DefModExtension
{
    public BorUISchemeDef? scheme;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        if (scheme == null)
        {
            yield return "BorUIScheme: no <scheme> given";
        }
    }
}

// A colour scheme for BOR's windows and for the tabs of buildings that name it (BorUIScheme). Only a skin that can
// recolour uses it (the UI Rethemed one, which swaps its palette for these colours while they draw); the vanilla look
// ignores it. Colours are hex ("E3A93B" or "#E3A93B"); any left out keep the player's own theme colour.
// The slots follow UI Rethemed's palette: surfaces from darkest (well) to lightest (panel), and four accents.
public class BorUISchemeDef : Def
{
    public string? well;        // sunken areas, tab title bar
    public string? card;        // cards, buttons, rows
    public string? panel;       // raised panels and borders
    public string? window;      // window background
    public string? titleBar;    // window title bar
    public string? titleAccent; // window title line; defaults to primary
    public string? primary;     // selection, confirm buttons, tab title line and pattern ("green" in the theme)
    public string? info;        // informational accent ("blue")
    public string? warn;        // warning accent ("yellow")
    public string? danger;      // destructive accent ("red")
    public string? neutral;     // plain buttons ("gray")
    public string? text;        // main text
    public string? textDim;     // secondary text

    // Background pattern instead of the theme's honeycomb (title bars, slots, lit toggles, hatching):
    // Honeycomb, Glow, Lines, Crosshatch, Grid, Dots, Stars or Plain.
    public string? pattern;

    // Or a pattern of our own: a texture path (white on transparent, seamless; tinted and faded like the theme's own
    // patterns), drawn as tiles of patternTileSize pixels (0: the texture's width). Takes precedence over pattern.
    public string? patternTexture;
    public float patternTileSize;

    // An animated pattern: patternTexture is then the frames' common path, frames patternTexture_00, _01, ... played
    // at patternFps (held on the first frame when the theme's animations are off). 0: a single texture.
    public int patternFrames;
    public float patternFps = 10f;

    private static readonly string[] Patterns = { "Honeycomb", "Glow", "Lines", "Crosshatch", "Grid", "Dots", "Stars", "Plain" };

    public static Color? Parse(string? hex)
    {
        if (hex.NullOrEmpty())
        {
            return null;
        }
        return GenColor.FromHex(hex);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors())
        {
            yield return error;
        }
        foreach ((string name, string? value) in new[]
                 {
                     (nameof(well), well), (nameof(card), card), (nameof(panel), panel), (nameof(window), window),
                     (nameof(titleBar), titleBar), (nameof(titleAccent), titleAccent), (nameof(primary), primary),
                     (nameof(info), info), (nameof(warn), warn), (nameof(danger), danger), (nameof(neutral), neutral),
                     (nameof(text), text), (nameof(textDim), textDim)
                 })
        {
            if (!value.NullOrEmpty() && !IsHex(value!))
            {
                yield return $"{name}: \"{value}\" is not a hex colour (RRGGBB or RRGGBBAA)";
            }
        }
        if (!pattern.NullOrEmpty() && !Patterns.Contains(pattern, StringComparer.OrdinalIgnoreCase))
        {
            yield return $"pattern: \"{pattern}\" is not one of {string.Join(", ", Patterns)}";
        }
    }

    private static bool IsHex(string value)
    {
        string hex = value.StartsWith("#") ? value.Substring(1) : value;
        return (hex.Length == 6 || hex.Length == 8) && int.TryParse(hex.Substring(0, 6), NumberStyles.HexNumber, null, out _);
    }
}

[DefOf]
public static class BOR_UISchemeDefOf
{
    public static BorUISchemeDef BOR_UIScheme_Solar = null!;
    public static BorUISchemeDef BOR_UIScheme_Kwazaari = null!;
    public static BorUISchemeDef BOR_UIScheme_Tenebral = null!;
    public static BorUISchemeDef BOR_UIScheme_Orientis = null!;

    static BOR_UISchemeDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_UISchemeDefOf));
    }
}
