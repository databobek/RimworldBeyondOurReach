using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// What a control means, so a skin can colour it: vanilla ignores it, a theme picks its own accent.
public enum BorTone
{
    Neutral,
    Good,
    Bad,
    Warn,
    Info
}

// The drawing primitives BOR's custom windows and tabs are built from. This class is the vanilla look; the optional
// UI Rethemed assembly (Mods/UIRethemed, loaded only with [LFS] UI Rethemed - Core) swaps in a subclass that draws the
// same primitives with that mod's widgets. Layout and logic stay in the windows themselves.
public class BorSkin
{
    public static readonly BorSkin Vanilla = new();

    public static BorSkin Current { get; private set; } = Vanilla;

    public static bool Themed => Current.IsThemed;

    public virtual bool IsThemed => false;

    // Called by a skin assembly once its hooks are in place.
    public static void Use(BorSkin skin)
    {
        Current = skin;
    }

    // A themed draw failed: back to vanilla for the rest of the session, with one warning. Only failures that come
    // from the skin itself (its assembly, the theme's assembly, or their code no longer matching) switch the theme
    // off; anything else is a normal bug and is left to the caller's error handling.
    public static void Blame(Exception e)
    {
        if (Current == Vanilla || !FromSkin(e))
        {
            return;
        }
        Current = Vanilla;
        Log.Warning($"[BOR] The UI Rethemed look failed, BOR windows use the vanilla look from now on: {e}");
    }

    private static bool FromSkin(Exception e)
    {
        if (e is TypeLoadException or MissingMemberException or TypeInitializationException)
        {
            return true;
        }
        var skinAssembly = Current.GetType().Assembly;
        foreach (StackFrame frame in new StackTrace(e).GetFrames())
        {
            var assembly = frame.GetMethod()?.DeclaringType?.Assembly;
            if (assembly == skinAssembly || assembly?.GetName().Name == "UIRethemedCore")
            {
                return true;
            }
        }
        return false;
    }

    // ---- Colour schemes ----

    // Recolours everything drawn until the matching PopScheme (null: keep the current colours). Calls always come in
    // pairs, PopScheme in a finally. The vanilla look has no palette to swap and ignores schemes.
    public virtual void PushScheme(BorUISchemeDef? scheme)
    {
    }

    public virtual void PopScheme()
    {
    }

    // A colour for a meaning, for things BOR paints itself (overdrive handle, overlays).
    public virtual Color ToneColor(BorTone tone) => tone switch
    {
        BorTone.Good => new Color(0.35f, 0.75f, 0.35f),
        BorTone.Bad => new Color(0.9f, 0.2f, 0.2f),
        BorTone.Warn => new Color(1f, 0.85f, 0.4f),
        BorTone.Info => new Color(0.4f, 0.6f, 0.9f),
        _ => Color.gray
    };

    // ---- Frames ----

    // Height a themed tab adds on top of its content for the title bar.
    public virtual float TabTitleHeight => 0f;

    // Draws the tab's frame and returns the rect to lay the content out in.
    public virtual Rect BeginTab(Rect rect, string title) => rect;

    public virtual void EndTab(Rect rect)
    {
    }

    // Draws the window's frame and title and returns the rect below it.
    public virtual Rect BeginWindow(Window window, Rect inRect, string title)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 36f), title);
        Text.Font = GameFont.Small;
        return new Rect(inRect.x, inRect.y + 40f, inRect.width, inRect.height - 40f);
    }

    public virtual void EndWindow(Window window, Rect inRect)
    {
    }

    // Background of a floating card (a window of its own). Vanilla lets the window draw its usual background; a themed
    // skin draws its own and the window's is switched off (see IsThemed).
    public virtual void Popup(Rect rect)
    {
    }

    // ---- Surfaces ----

    // A framed area for lists and cards.
    public virtual void Section(Rect rect) => Widgets.DrawMenuSection(rect);

    // A grid cell or track; the colours are the vanilla ones, a theme may only take their hue.
    public virtual void Cell(Rect rect, Color back, Color border)
    {
        Widgets.DrawBoxSolid(rect, back);
        Box(rect, border);
    }

    // A list row: alternate rows are shaded, the selected one marked, hoverable ones lit under the mouse.
    public virtual void Row(Rect rect, bool selected, bool hoverable = true, bool alternate = false)
    {
        if (alternate)
        {
            Widgets.DrawLightHighlight(rect);
        }
        if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }
        else if (hoverable)
        {
            Widgets.DrawHighlightIfMouseover(rect);
        }
    }

    public virtual void Fill(Rect rect, Color color) => Widgets.DrawBoxSolid(rect, color);

    // A translucent overlay (progress, a marked column): vanilla keeps its own colour, a theme may use the tone's.
    public virtual void Overlay(Rect rect, Color vanilla, BorTone tone) => Widgets.DrawBoxSolid(rect, vanilla);

    public virtual void Box(Rect rect, Color color)
    {
        GUI.color = color;
        Widgets.DrawBox(rect);
        GUI.color = Color.white;
    }

    public virtual void Hover(Rect rect) => Widgets.DrawHighlightIfMouseover(rect);

    // Background of a small clickable label inside a cell.
    public virtual void ChipFace(Rect rect) => Widgets.DrawAtlas(rect, ForgeTextures.TabAtlas);

    // ---- Controls ----

    public virtual bool Button(Rect rect, string label, bool active = true, BorTone tone = BorTone.Neutral, bool selected = false)
    {
        bool clicked = Widgets.ButtonText(rect, label, active: active);
        if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }
        return clicked;
    }

    public virtual void Checkbox(Rect rect, string label, ref bool on) => Widgets.CheckboxLabeled(rect, label, ref on);

    // A progress bar with a left-aligned label on it.
    public virtual void Bar(Rect rect, float fraction, string label, BorTone tone = BorTone.Neutral)
    {
        Widgets.FillableBar(rect, fraction);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x + 4f, rect.y, rect.width - 4f, rect.height), label);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    // ---- Text colours ----

    public virtual Color TextDim => Color.gray;

    public virtual Color TextBad => ColorLibrary.RedReadable;

    public virtual Color TextWarn => new(1f, 0.85f, 0.4f);
}
