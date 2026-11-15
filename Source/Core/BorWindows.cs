using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

// Base of BOR's dialogs: BorSkin.Current draws the frame and title, the subclass draws Contents in the rect below.
// With a themed skin the vanilla background, shadow, margin and close X are switched off (the skin draws its own);
// if the theme is dropped mid-session they come back on the next frame.
public abstract class BorWindow : Window
{
    private bool themedFlags;
    private bool vanillaCloseX;
    private bool vanillaBackground;
    private bool vanillaShadow;

    protected abstract string Title { get; }

    // Colours for this window under a skin that can recolour; null keeps the theme's.
    protected virtual BorUISchemeDef? Scheme => null;

    protected override float Margin => themedFlags ? 0f : base.Margin;

    public override void PreOpen()
    {
        base.PreOpen();
        ApplyLook();
    }

    private void ApplyLook()
    {
        bool themed = BorSkin.Themed;
        if (themed == themedFlags)
        {
            return;
        }
        if (themed)
        {
            (vanillaCloseX, vanillaBackground, vanillaShadow) = (doCloseX, doWindowBackground, drawShadow);
            (doCloseX, doWindowBackground, drawShadow) = (false, false, false);
        }
        else
        {
            (doCloseX, doWindowBackground, drawShadow) = (vanillaCloseX, vanillaBackground, vanillaShadow);
        }
        themedFlags = themed;
    }

    public sealed override void DoWindowContents(Rect inRect)
    {
        ApplyLook();
        BorSkin skin = BorSkin.Current;
        skin.PushScheme(Scheme);
        try
        {
            Contents(skin.BeginWindow(this, inRect, Title));
        }
        catch (Exception e)
        {
            BorSkin.Blame(e);
            throw;
        }
        finally
        {
            try
            {
                skin.EndWindow(this, inRect);
            }
            catch (Exception e)
            {
                BorSkin.Blame(e);
            }
            skin.PopScheme();
        }
    }

    protected abstract void Contents(Rect inRect);
}

// Base of BOR's inspector tabs: BorSkin.Current draws the frame (a themed skin adds a title bar, see TitleHeight),
// the subclass draws Contents in the rect inside it. A themed skin's vanilla window background is switched off by the
// skin assembly.
public abstract class BorTab : ITab
{
    // Extra height the frame needs on top of the content; add it when sizing the tab.
    protected static float TitleHeight => BorSkin.Current.TabTitleHeight;

    protected virtual string Title => SelThing?.LabelCap ?? labelKey.Translate().Resolve();

    // Colours for this tab under a skin that can recolour: the scheme the selected building's BorUIScheme names.
    protected virtual BorUISchemeDef? Scheme => SelThing?.def.GetModExtension<BorUIScheme>()?.scheme;

    protected sealed override void FillTab()
    {
        BorSkin skin = BorSkin.Current;
        Rect rect = new(0f, 0f, size.x, size.y);
        skin.PushScheme(Scheme);
        try
        {
            Contents(skin.BeginTab(rect, Title));
        }
        catch (Exception e)
        {
            BorSkin.Blame(e);
            throw;
        }
        finally
        {
            try
            {
                skin.EndTab(rect);
            }
            catch (Exception e)
            {
                BorSkin.Blame(e);
            }
            skin.PopScheme();
        }
    }

    protected abstract void Contents(Rect rect);
}
