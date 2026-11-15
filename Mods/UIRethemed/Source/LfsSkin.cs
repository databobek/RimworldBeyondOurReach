using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using UIRethemed.Core;
using UnityEngine;
using Verse;

namespace BeyondOurReach.UIRethemed;

/// <summary>
/// [LFS] UI Rethemed (Core) is a UI theme. This assembly only loads when its Core is active (see LoadFolders.xml) and
/// makes BOR's custom windows and inspector tabs use it: BorSkin primitives are redrawn with the theme's widgets, and
/// the frames follow the theme's own popups and inspector tabs (rounded window, hex title bar, header close button).
/// BorSkin.Blame drops back to the vanilla look if anything here stops working after a theme update.
/// </summary>
[StaticConstructorOnStartup]
public static class UIRethemedCompat
{
	private const int TabWindowId = 235086; // InspectTabBase.DoTabGUI's ImmediateWindow

	static UIRethemedCompat()
	{
		try
		{
			var harmony = new Harmony("bambaryla.bor.redux.uirethemed");
			harmony.Patch(AccessTools.Method(typeof(InspectTabBase), nameof(InspectTabBase.DoTabGUI)),
				prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(DoTabGUIPrefix)),
				finalizer: new HarmonyMethod(typeof(UIRethemedCompat), nameof(DoTabGUIFinalizer)));
			harmony.Patch(AccessTools.Method(typeof(WindowStack), nameof(WindowStack.ImmediateWindow)),
				prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(ImmediateWindowPrefix)));
			BorSkin.Use(new LfsSkin());
			Log.Message("[BOR] UI Rethemed detected: BOR windows and tabs use its theme.");
			HookCustomTiles(harmony);
			HookBills(harmony);
		}
		catch (Exception e)
		{
			Log.Warning($"[BOR] Could not hook into UI Rethemed, BOR windows keep the vanilla look: {e}");
		}
	}

	// Every pattern the theme draws (title bars, slots, toggles, hatching) asks its internal Patterns class for the
	// tile texture and size; while a scheme with its own texture is pushed, these answer with ours instead.
	private static void HookCustomTiles(Harmony harmony)
	{
		try
		{
			Type patterns = AccessTools.TypeByName("UIRethemed.Core.Patterns") ?? throw new MissingMemberException("UIRethemed.Core.Patterns");
			harmony.Patch(AccessTools.Method(patterns, "Tile") ?? throw new MissingMethodException("Patterns", "Tile"),
				prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(TilePrefix)));
			harmony.Patch(AccessTools.Method(patterns, "TileSize") ?? throw new MissingMethodException("Patterns", "TileSize"),
				prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(TileSizePrefix)));
			LfsSkin.CustomTilesHooked = true;
		}
		catch (Exception e)
		{
			Log.Warning($"[BOR] Could not hook UI Rethemed's patterns, schemes use its own patterns only: {e}");
		}
	}

	private static bool TilePrefix(ref Texture2D __result)
	{
		if (LfsSkin.CustomTiles is { Length: > 0 } tiles)
		{
			// An animated pattern plays its frames (rising flames); with the theme's animations off it holds the first.
			int frame = tiles.Length > 1 && ThemeMotion.On ? (int)(Time.realtimeSinceStartup * LfsSkin.CustomFps) % tiles.Length : 0;
			__result = tiles[frame];
			return false;
		}
		return true;
	}

	private static bool TileSizePrefix(ref Vector2 __result)
	{
		if (LfsSkin.CustomTiles != null)
		{
			__result = LfsSkin.CustomTileSize;
			return false;
		}
		return true;
	}

	// ---- Inspector tabs ----
	// BOR's own tabs (BorTab) frame and recolour themselves. Other tabs on a building with a BorUIScheme are recoloured
	// by running their window function inside its scheme: the resource drill tab (drawn with the theme by Resource
	// Generator Framework) picks that up as is, and the vanilla bills tab additionally gets the theme's frame and
	// restyled buttons and rows (see FillTab hooks below).

	private static readonly AccessTools.FieldRef<InspectTabBase, Vector2> tabSize = AccessTools.FieldRefAccess<InspectTabBase, Vector2>("size");
	private static Vector2? billsVanillaSize;
	private static bool frameOnly;            // a tab whose frame the skin draws: no vanilla window background
	private static BorUISchemeDef? tabScheme; // scheme for a tab that does not push its own

	// Find.Selector only exists in a running game (UIRoot_Play); the world map's start-site page has none.
	private static BorUISchemeDef? SelectedScheme() => Current.ProgramState == ProgramState.Playing
		? Find.Selector.SingleSelectedThing?.def.GetModExtension<BorUIScheme>()?.scheme
		: null;

	private static void DoTabGUIPrefix(InspectTabBase __instance)
	{
		bool themed = BorSkin.Themed;
		// Only map inspector tabs (ITab); world tabs (WITab) have nothing to do with the selected building.
		BorUISchemeDef? scheme = themed && __instance is ITab ? SelectedScheme() : null;
		bool bills = __instance is ITab_Bills && !NiceBillTabActive;
		frameOnly = themed && (__instance is BorTab || (bills && scheme != null));
		tabScheme = __instance is BorTab ? null : scheme;
		if (bills)
		{
			// One ITab_Bills serves every bench: taller (for the title bar) only on a bench with a scheme.
			billsVanillaSize ??= tabSize(__instance);
			tabSize(__instance) = billsVanillaSize.Value + (frameOnly ? new Vector2(0f, BorSkin.Current.TabTitleHeight) : Vector2.zero);
		}
	}

	private static void DoTabGUIFinalizer() => (frameOnly, tabScheme) = (false, null);

	// The window function may run after DoTabGUI returns, so the scheme travels inside it.
	private static void ImmediateWindowPrefix(int ID, ref bool doBackground, ref Action doWindowFunc)
	{
		if (ID != TabWindowId)
		{
			return;
		}
		if (frameOnly)
		{
			doBackground = false;
		}
		if (tabScheme is { } scheme)
		{
			Action inner = doWindowFunc;
			doWindowFunc = () =>
			{
				BorSkin skin = BorSkin.Current;
				skin.PushScheme(scheme);
				try
				{
					inner();
				}
				finally
				{
					skin.PopScheme();
				}
			};
		}
	}

	// ---- Bills tab ----
	// Vanilla draws it; on a bench with a scheme it is drawn inside the theme's tab frame, and while it draws the few
	// vanilla widgets it uses (text buttons, alternating row backgrounds) are drawn with the theme instead. The bills
	// code itself is untouched, so other mods' bill patches keep working.

	private static bool billsFramed;
	private static Rect billsRect;
	private static int restyling;

	// Nice Bill Tab replaces the bills tab with its own look; leave the tab completely alone when it is loaded.
	private static readonly bool NiceBillTabActive = ModsConfig.IsActive("Andromeda.NiceBillTab");

	private static void HookBills(Harmony harmony)
	{
		if (NiceBillTabActive)
		{
			Log.Message("[BOR] Nice Bill Tab detected: the bills tab is left to it.");
			return;
		}
		try
		{
			harmony.Patch(AccessTools.Method(typeof(ITab_Bills), "FillTab"),
				prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(BillsFillTabPrefix)),
				finalizer: new HarmonyMethod(typeof(UIRethemedCompat), nameof(BillsFillTabFinalizer)));
			foreach (MethodInfo method in typeof(Widgets).GetMethods(BindingFlags.Static | BindingFlags.Public))
			{
				ParameterInfo[] p = method.GetParameters();
				if (method.Name == nameof(Widgets.ButtonText) && method.ReturnType == typeof(bool) && p.Length >= 2 && p[0].ParameterType == typeof(Rect)
				    && p[1].ParameterType == typeof(string) && p.Any(q => q.Name == "drawBackground") && p.Any(q => q.Name == "active"))
				{
					harmony.Patch(method, prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(ButtonTextPrefix)));
				}
			}
			harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.DrawAltRect), new[] { typeof(Rect) }),
				prefix: new HarmonyMethod(typeof(UIRethemedCompat), nameof(DrawAltRectPrefix)));
		}
		catch (Exception e)
		{
			Log.Warning($"[BOR] Could not restyle the bills tab, benches keep the vanilla bills tab: {e}");
		}
	}

	private static void BillsFillTabPrefix(ITab_Bills __instance)
	{
		billsFramed = false;
		Thing? bench = Find.Selector.SingleSelectedThing;
		if (!BorSkin.Themed || SelectedScheme() == null || bench == null)
		{
			return;
		}
		Vector2 size = tabSize(__instance);
		billsRect = new Rect(0f, 0f, size.x, size.y);
		Rect body = BorSkin.Current.BeginTab(billsRect, bench.LabelCap);
		Widgets.BeginGroup(body);
		billsFramed = true;
		restyling++;
	}

	private static void BillsFillTabFinalizer()
	{
		if (!billsFramed)
		{
			return;
		}
		billsFramed = false;
		restyling--;
		Widgets.EndGroup();
		try
		{
			BorSkin.Current.EndTab(billsRect);
		}
		catch (Exception e)
		{
			BorSkin.Blame(e);
		}
	}

	private static bool ButtonTextPrefix(Rect rect, string label, bool drawBackground, bool active, ref bool __result)
	{
		if (restyling == 0 || !drawBackground)
		{
			return true;
		}
		// Add bill is the tab's main action; the repeat mode label comes padded with spaces for vanilla's left alignment.
		BorTone tone = label == "AddBill".Translate() ? BorTone.Good : BorTone.Neutral;
		__result = BorSkin.Current.Button(rect, label.TrimEnd(), active, tone);
		return false;
	}

	private static bool DrawAltRectPrefix(Rect rect)
	{
		if (restyling == 0)
		{
			return true;
		}
		CoreWidgets.Card(rect.ContractedBy(1f), hoverable: false);
		return false;
	}
}

// Holds texture fields (the scheme's pattern tile), so RimWorld wants its statics set up on the main thread.
[StaticConstructorOnStartup]
public class LfsSkin : BorSkin
{
	private const float TabTitle = 26f;
	private const float WindowTitle = 40f;
	private const float WindowPad = 10f;
	// No UI Rethemed type may appear in a field: RimWorld lists this assembly's types as it loads it, possibly before
	// UI Rethemed itself is loaded (mod order), and a field's type is loaded right then. Method bodies are fine.
	private static Corners TabCorners => Corners.Top | Corners.BottomRight;

	// When each window was first drawn, for the title accent's grow-in.
	private readonly ConditionalWeakTable<Window, StrongBox<float>> windowOpened = new();
	private string? shownTab;
	private int shownTabFrame = -1;
	private float shownTabSince;

	public override bool IsThemed => true;

	// ---- Colour schemes ----
	// UI Rethemed keeps one palette (internal ThemeLook.current) that every widget reads. While a BOR tab or window with
	// a BorUISchemeDef draws, that field holds a copy recoloured with the scheme; the player's palette goes back right
	// after. These are internals, so anything missing just switches schemes off; the theme itself keeps working.

	private static readonly Type? themeLook = AccessTools.TypeByName("UIRethemed.Core.ThemeLook");
	private static readonly FieldInfo? currentPalette = themeLook == null ? null : AccessTools.Field(themeLook, "current");
	private static readonly PropertyInfo? builtPalette = themeLook == null ? null : AccessTools.Property(themeLook, "Current");
	private static readonly MethodInfo? textFor = themeLook == null ? null : AccessTools.Method(themeLook, "TextFor");
	// The pattern the theme draws is Previewing ?? the player's (its settings page previews a style through it).
	private static readonly FieldInfo? previewingPattern = themeLook == null ? null : AccessTools.Field(themeLook, "Previewing");

	// A scheme's own pattern texture, handed to the theme by UIRethemedCompat's patches on its pattern lookup
	// (Patterns.Tile / TileSize) while the scheme is pushed. Null: the theme's own tiles.
	internal static Texture2D[]? CustomTiles;
	internal static Vector2 CustomTileSize;
	internal static float CustomFps;
	internal static bool CustomTilesHooked;

	// What a PushScheme replaced, put back by the matching PopScheme.
	private sealed class Replaced
	{
		public object? Palette;
		public bool PatternSet;
		public object? Pattern; // a boxed PatternStyle?, see TabCorners
		public bool TileSet;
		public Texture2D[]? Tiles;
		public Vector2 TileSize;
		public float Fps;
	}

	private readonly Stack<Replaced> saved = new();
	private readonly Dictionary<BorUISchemeDef, (int version, object palette)> schemePalettes = new();
	private readonly Dictionary<BorUISchemeDef, Texture2D[]?> tileSets = new();
	private bool schemesBroken;

	public override void PushScheme(BorUISchemeDef? scheme)
	{
		var replaced = new Replaced();
		if (scheme != null && !schemesBroken)
		{
			try
			{
				if (currentPalette == null || builtPalette == null)
				{
					throw new MissingMemberException("UIRethemed.Core.ThemeLook", "current");
				}
				object player = builtPalette.GetValue(null)!; // builds the player's palette if it is stale
				currentPalette.SetValue(null, PaletteFor(scheme, player));
				replaced.Palette = player;

				Texture2D[]? tiles = CustomTilesHooked ? Tiles(scheme) : null;
				Texture2D? tile = tiles?[0];
				// Our texture is drawn in place of the honeycomb, so the theme must be drawing a tiled pattern.
				PatternStyle? style = tile != null ? PatternStyle.Honeycomb
					: !scheme.pattern.NullOrEmpty() && Enum.TryParse(scheme.pattern, true, out PatternStyle named) ? named
					: null;
				if (style != null)
				{
					if (previewingPattern == null)
					{
						throw new MissingMemberException("UIRethemed.Core.ThemeLook", "Previewing");
					}
					replaced.Pattern = previewingPattern.GetValue(null);
					replaced.PatternSet = true;
					previewingPattern.SetValue(null, style);
				}
				if (tile != null)
				{
					(replaced.TileSet, replaced.Tiles, replaced.TileSize, replaced.Fps) = (true, CustomTiles, CustomTileSize, CustomFps);
					float side = scheme.patternTileSize > 0f ? scheme.patternTileSize : tile.width;
					(CustomTiles, CustomTileSize, CustomFps) = (tiles, new Vector2(side, side * tile.height / tile.width), Mathf.Max(0.1f, scheme.patternFps));
				}
			}
			catch (Exception e)
			{
				schemesBroken = true;
				Restore(replaced);
				replaced = new Replaced();
				Log.Warning($"[BOR] UI Rethemed colour schemes are off, BOR windows use the player's theme colours: {e}");
			}
		}
		saved.Push(replaced);
	}

	public override void PopScheme()
	{
		if (saved.Count > 0)
		{
			Restore(saved.Pop());
		}
	}

	private static void Restore(Replaced replaced)
	{
		if (replaced.TileSet)
		{
			(CustomTiles, CustomTileSize, CustomFps) = (replaced.Tiles, replaced.TileSize, replaced.Fps);
		}
		if (replaced.PatternSet)
		{
			previewingPattern!.SetValue(null, replaced.Pattern);
		}
		if (replaced.Palette != null)
		{
			currentPalette!.SetValue(null, replaced.Palette);
		}
	}

	// Loaded once per path; a missing texture warns once and the scheme falls back to its named pattern.
	// The scheme's pattern frames, loaded once per scheme: patternTexture itself, or for patternFrames > 0 the frames
	// patternTexture_00, _01, ... A missing texture warns once and the scheme falls back to its named pattern.
	private Texture2D[]? Tiles(BorUISchemeDef scheme)
	{
		if (scheme.patternTexture.NullOrEmpty())
		{
			return null;
		}
		if (!tileSets.TryGetValue(scheme, out Texture2D[]? tiles))
		{
			string path = scheme.patternTexture!;
			string[] paths = scheme.patternFrames > 0
				? Enumerable.Range(0, scheme.patternFrames).Select(i => $"{path}_{i:D2}").ToArray()
				: new[] { path };
			Texture2D[] loaded = paths.Select(p => ContentFinder<Texture2D>.Get(p, reportFailure: false)).ToArray();
			string? missing = paths.Where((_, i) => loaded[i] == null).FirstOrDefault();
			if (missing != null)
			{
				Log.Warning($"[BOR] UI scheme {scheme.defName}: pattern texture \"{missing}\" not found, using its named pattern instead.");
			}
			tiles = missing == null ? loaded : null;
			tileSets[scheme] = tiles;
		}
		return tiles;
	}

	// The player's palette with the scheme's colours on top, rebuilt when the player changes their theme colours.
	private object PaletteFor(BorUISchemeDef scheme, object player)
	{
		int version = Theme.LookVersion;
		if (schemePalettes.TryGetValue(scheme, out var cached) && cached.version == version)
		{
			return cached.palette;
		}
		object palette = AccessTools.Method(player.GetType(), "Copy").Invoke(player, null)!;
		Color Get(string field) => (Color)AccessTools.Field(palette.GetType(), field).GetValue(palette)!;
		void Set(string field, Color? color)
		{
			if (color is { } c)
			{
				AccessTools.Field(palette.GetType(), field).SetValue(palette, c);
			}
		}

		Set("BG", BorUISchemeDef.Parse(scheme.card));
		Set("BGLight", BorUISchemeDef.Parse(scheme.panel));
		Set("BGDark", BorUISchemeDef.Parse(scheme.well));
		Set("Window", BorUISchemeDef.Parse(scheme.window));
		Set("TitleBar", BorUISchemeDef.Parse(scheme.titleBar));
		Set("HighlightGray", BorUISchemeDef.Parse(scheme.neutral));
		Color card = Get("BG");
		// Each accent also sets its readable text colour and the faint card tint the theme derives from it.
		void SetAccent(string name, string? hex)
		{
			if (BorUISchemeDef.Parse(hex) is not { } accent)
			{
				return;
			}
			Set("Highlight" + name, accent);
			Set("Text" + name, textFor?.Invoke(null, new object[] { accent }) as Color? ?? Color.Lerp(accent, Color.white, 0.45f));
			Set("BG" + name, Color.Lerp(card, accent, 0.25f));
		}
		SetAccent("Green", scheme.primary);
		SetAccent("Blue", scheme.info);
		SetAccent("Yellow", scheme.warn);
		SetAccent("Red", scheme.danger);
		Set("Info", BorUISchemeDef.Parse(scheme.info));
		Set("TitleAccent", BorUISchemeDef.Parse(scheme.titleAccent) ?? BorUISchemeDef.Parse(scheme.primary));
		Set("TextBright", BorUISchemeDef.Parse(scheme.text));
		if (BorUISchemeDef.Parse(scheme.textDim) is { } dim)
		{
			Set("TextDim", dim);
			Set("TextFaint", Color.Lerp(dim, card, 0.4f));
		}
		schemePalettes[scheme] = (version, palette);
		return palette;
	}

	public override Color ToneColor(BorTone tone) => Accent(tone);

	public override void Overlay(Rect rect, Color vanilla, BorTone tone)
	{
		Color accent = Accent(tone);
		Rounded.Fill(rect, new Color(accent.r, accent.g, accent.b, vanilla.a * 0.55f));
	}

	private static Color Accent(BorTone tone) => tone switch
	{
		BorTone.Good => Theme.HighlightGreen,
		BorTone.Bad => Theme.HighlightRed,
		BorTone.Warn => Theme.HighlightYellow,
		BorTone.Info => Theme.HighlightBlue,
		_ => Theme.HighlightGray
	};

	private static float Animated(float age) => ThemeMotion.On ? age : float.MaxValue;

	// ---- Frames ----

	public override float TabTitleHeight => TabTitle;

	// Same frame as UI Rethemed's inspector tabs: rounded window, dark hex title bar with a growing accent line.
	public override Rect BeginTab(Rect rect, string title)
	{
		if (title != shownTab || Time.frameCount - shownTabFrame > 2)
		{
			shownTab = title;
			shownTabSince = ThemeMotion.Now;
		}
		shownTabFrame = Time.frameCount;

		Rounded.WindowBackground(rect, TabCorners);
		var bar = new Rect(rect.x, rect.y, rect.width, TabTitle);
		Rounded.Fill(bar, Theme.BGDark, Corners.Top);
		Faces.FadingHex(bar, Theme.HighlightGreen, Theme.BGDark, 1f, 0.6f);
		CoreWidgets.Accent(bar, Theme.HighlightGreen, Animated(ThemeMotion.Now - shownTabSince));
		CoreWidgets.Line(new Rect(bar.x + 10f, bar.y, bar.width - 40f, bar.height), title, Color.white);
		return new Rect(rect.x, bar.yMax, rect.width, rect.height - TabTitle);
	}

	public override void EndTab(Rect rect)
	{
		Theme.CloseScrolls();
		Theme.Reset();
		Rounded.Border(rect, Rounded.WindowBorderColor, TabCorners);
		// Vanilla's close button sits under this and takes the click.
		CoreWidgets.IconButton(new Rect(rect.width - 22f, 4f, 18f, 18f), CoreWidgets.CloseTex, "CloseButton".Translate(), iconSize: 14f);
		Theme.Reset();
	}

	// Same frame as UI Rethemed's popups: rounded window, title bar in the title colours, close button in the header.
	public override Rect BeginWindow(Window window, Rect inRect, string title)
	{
		float opened = windowOpened.GetValue(window, _ => new StrongBox<float>(ThemeMotion.Now)).Value;
		var bar = new Rect(inRect.x, inRect.y, inRect.width, WindowTitle);
		Rounded.BeginBatch();
		try
		{
			Rounded.WindowBackground(inRect);
			Rounded.Fill(bar, Theme.TitleBarFill, Corners.Top);
		}
		finally
		{
			Rounded.EndBatch();
		}
		Faces.FadingHex(bar, Theme.TitleAccent, Theme.TitleBarFill, 1f, 0.6f);
		CoreWidgets.Accent(bar, Theme.TitleAccent, Animated(ThemeMotion.Now - opened));
		float right = CoreWidgets.HeaderButtons(bar, () => window.Close());
		CoreWidgets.Line(new Rect(bar.x + 14f, bar.y, Mathf.Max(0f, right - bar.x - 18f), bar.height), title, Color.white, GameFont.Medium);
		return new Rect(inRect.x + WindowPad, bar.yMax + 8f, inRect.width - WindowPad * 2f, inRect.yMax - bar.yMax - 8f - WindowPad);
	}

	public override void EndWindow(Window window, Rect inRect)
	{
		Theme.CloseScrolls();
		Theme.Reset();
		Rounded.Border(inRect, Rounded.WindowBorderColor);
	}

	public override void Popup(Rect rect)
	{
		Rounded.WindowBackground(rect);
		Rounded.Fill(new Rect(rect.x + 10f, rect.y, rect.width - 20f, 2f), Faces.Fade(Theme.HighlightGreen, 0.8f));
	}

	// ---- Surfaces ----

	public override void Section(Rect rect)
	{
		Rounded.BeginBatch();
		try
		{
			Rounded.Fill(rect, Theme.BG);
			Rounded.Border(rect, Theme.BGDark);
		}
		finally
		{
			Rounded.EndBatch();
		}
	}

	// The theme's dark cell, tinted towards the vanilla colours so a red altar stays red and a grey forge grey.
	public override void Cell(Rect rect, Color back, Color border)
	{
		Rounded.BeginBatch();
		try
		{
			// Only a touch of the vanilla hue (from the border; the vanilla backs are near-black and would just grey the
			// theme's surface out), so a scheme's surfaces still show through.
			Rounded.Fill(rect, Color.Lerp(Theme.BGDark, border, 0.12f));
			Rounded.Border(rect, Color.Lerp(Theme.BGLight, border, 0.35f));
		}
		finally
		{
			Rounded.EndBatch();
		}
	}

	// Rows are the theme's cards: lit when selected, brightening under the mouse when they can be clicked.
	public override void Row(Rect rect, bool selected, bool hoverable = true, bool alternate = false)
	{
		CoreWidgets.Card(rect.ContractedBy(1f), selected, Theme.HighlightGreen, hoverable);
	}

	public override void Fill(Rect rect, Color color) => Rounded.Fill(rect, color);

	public override void Box(Rect rect, Color color) => Rounded.Border(rect, color);

	public override void Hover(Rect rect)
	{
		if (Mouse.IsOver(rect))
		{
			Rounded.Fill(rect.ContractedBy(1f), new Color(1f, 1f, 1f, 0.07f));
		}
	}

	public override void ChipFace(Rect rect)
	{
		Rounded.BeginBatch();
		try
		{
			Rounded.Fill(rect, Theme.BG);
			Rounded.Border(rect, Theme.BGLight);
		}
		finally
		{
			Rounded.EndBatch();
		}
	}

	// ---- Controls ----

	public override bool Button(Rect rect, string label, bool active = true, BorTone tone = BorTone.Neutral, bool selected = false) =>
		CoreWidgets.Button(rect, label, accent: Accent(tone), enabled: active, on: selected, raised: tone == BorTone.Good && active);

	// A card with the theme's switch on the right; the whole row toggles.
	public override void Checkbox(Rect rect, string label, ref bool on)
	{
		var card = rect.ContractedBy(1f);
		CoreWidgets.Card(card, on, Theme.HighlightGreen);
		var switchRect = new Rect(card.xMax - 36f, Mathf.Round(card.center.y - 7f), 28f, 14f);
		Theme.Switch(switchRect, on);
		CoreWidgets.Line(new Rect(card.x + 8f, card.y, switchRect.x - card.x - 16f, card.height), label, on ? Color.white : Theme.TextBright);
		if (Widgets.ButtonInvisible(rect))
		{
			on = !on;
		}
	}

	public override void Bar(Rect rect, float fraction, string label, BorTone tone = BorTone.Neutral)
	{
		Theme.Bar(rect, fraction, Accent(tone));
		CoreWidgets.Line(new Rect(rect.x + 6f, rect.y, rect.width - 12f, rect.height), label, Color.white);
	}

	// ---- Text colours ----

	public override Color TextDim => Theme.TextDim;

	public override Color TextBad => Theme.TextRed;

	public override Color TextWarn => Theme.TextYellow;
}
