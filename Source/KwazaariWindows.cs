using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeyondOurReach;

public static class KwazaariUI
{
    public static string Scrip(float amount) => "BOR_Kwazaari_ScripAmount".Translate(amount.ToString("0.##"));

    public static void DrawHeader(Rect rect, string title)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, 32f), title);
        Text.Font = GameFont.Small;
        float scrip = GameComponent_Kwazaari.Instance.Scrip;
        Rect balance = new Rect(rect.x, rect.y + 36f, rect.width, 24f);
        GUI.color = scrip < 0f ? ColorLibrary.RedReadable : Color.white;
        Widgets.LabelWithIcon(balance, "BOR_Kwazaari_Balance".Translate(Scrip(scrip)), KwazaariTex.Scrip);
        GUI.color = Color.white;
    }
}

// Opened from the comm console: sell technology or spend scrip.
public class Dialog_Kwazaari : Window
{
    private readonly Thing console;

    public Dialog_Kwazaari(Thing console)
    {
        this.console = console;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        closeOnClickedOutside = true;
    }

    public override Vector2 InitialSize => new Vector2(460f, 230f);

    public override void DoWindowContents(Rect inRect)
    {
        KwazaariUI.DrawHeader(inRect, "BOR_Kwazaari_Title".Translate());
        float y = inRect.y + 80f;
        float w = (inRect.width - 10f) / 2f;
        Rect sell = new Rect(inRect.x, y, w, 60f);
        Rect spend = new Rect(sell.xMax + 10f, y, w, 60f);
        TooltipHandler.TipRegion(sell, "BOR_Kwazaari_Sell_Tip".Translate());
        TooltipHandler.TipRegion(spend, "BOR_Kwazaari_Spend_Tip".Translate());
        if (Widgets.ButtonText(sell, "BOR_Kwazaari_Sell".Translate()))
        {
            Close();
            Find.WindowStack.Add(new Dialog_KwazaariSell(console.Map));
        }
        if (Widgets.ButtonText(spend, "BOR_Kwazaari_Spend".Translate()))
        {
            Close();
            Find.WindowStack.Add(new Dialog_KwazaariFavors(console));
        }
    }
}

// Lists sellable things in the home area grouped by def, stuff and quality; sold things are skipped away.
public class Dialog_KwazaariSell : Window
{
    private const float RowHeight = 32f;

    private class Row
    {
        public List<Thing> things = new();
        public string label = "";
        public int available;
        public int selected;

        public Thing Sample => things[0];

        // Scrip for the first `count` units, taken in list order.
        public float Value(int count)
        {
            float total = 0f;
            foreach (Thing t in things)
            {
                if (count <= 0)
                {
                    break;
                }
                int take = Mathf.Min(count, t.stackCount);
                total += KwazaariUtility.UnitScrip(t) * take;
                count -= take;
            }
            return total;
        }
    }

    private readonly Map map;
    private readonly List<Row> rows;
    private Vector2 scroll;

    public Dialog_KwazaariSell(Map map)
    {
        this.map = map;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        rows = BuildRows(map);
    }

    public override Vector2 InitialSize => new Vector2(720f, 640f);

    private static List<Row> BuildRows(Map map)
    {
        List<Row> result = new();
        foreach (IGrouping<(ThingDef, ThingDef, QualityCategory?), Thing> group in KwazaariUtility.SellableThings(map)
                     .GroupBy(t => (t.def, t.Stuff, t.TryGetQuality(out QualityCategory q) ? q : (QualityCategory?)null)))
        {
            List<Thing> things = group.ToList();
            result.Add(new Row
            {
                things = things,
                label = things[0].LabelCapNoCount,
                available = things.Sum(t => t.stackCount)
            });
        }
        return result.OrderBy(r => r.label).ToList();
    }

    private float Total => rows.Sum(r => r.Value(r.selected));

    public override void DoWindowContents(Rect inRect)
    {
        KwazaariUI.DrawHeader(inRect, "BOR_Kwazaari_Sell".Translate());
        Rect listRect = new Rect(inRect.x, inRect.y + 70f, inRect.width, inRect.height - 70f - 50f);
        if (rows.Count == 0)
        {
            Widgets.NoneLabelCenteredVertically(listRect, "BOR_Kwazaari_NothingToSell".Translate());
        }
        else
        {
            Rect view = new Rect(0f, 0f, listRect.width - 16f, rows.Count * RowHeight);
            Widgets.BeginScrollView(listRect, ref scroll, view);
            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(new Rect(0f, i * RowHeight, view.width, RowHeight), rows[i], i);
            }
            Widgets.EndScrollView();
        }

        Rect bottom = new Rect(inRect.x, inRect.yMax - 40f, inRect.width, 40f);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.LabelWithIcon(new Rect(bottom.x, bottom.y, 300f, bottom.height), "BOR_Kwazaari_SaleTotal".Translate(KwazaariUI.Scrip(Total)), KwazaariTex.Scrip);
        Text.Anchor = TextAnchor.UpperLeft;
        if (Widgets.ButtonText(new Rect(bottom.xMax - 330f, bottom.y, 160f, bottom.height), "CancelButton".Translate()))
        {
            Close();
        }
        if (Widgets.ButtonText(new Rect(bottom.xMax - 160f, bottom.y, 160f, bottom.height), "BOR_Kwazaari_ConfirmSale".Translate(), active: Total > 0f))
        {
            Sell();
        }
    }

    private void DrawRow(Rect rect, Row row, int index)
    {
        if (index % 2 == 1)
        {
            Widgets.DrawLightHighlight(rect);
        }
        Widgets.InfoCardButton(rect.x, rect.y + 4f, row.Sample);
        Widgets.ThingIcon(new Rect(rect.x + 28f, rect.y + 2f, 28f, 28f), row.Sample);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.LabelEllipses(new Rect(rect.x + 62f, rect.y, 250f, rect.height), row.label);
        Widgets.Label(new Rect(rect.x + 316f, rect.y, 60f, rect.height), "x" + row.available);
        Widgets.Label(new Rect(rect.x + 376f, rect.y, 100f, rect.height), KwazaariUI.Scrip(row.Value(1)));
        Text.Anchor = TextAnchor.UpperLeft;

        float x = rect.xMax - 190f;
        int step = GenUI.CurrentAdjustmentMultiplier();
        if (Widgets.ButtonText(new Rect(x, rect.y + 3f, 30f, 26f), "-"))
        {
            row.selected = Mathf.Max(0, row.selected - step);
        }
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(new Rect(x + 30f, rect.y, 60f, rect.height), row.selected.ToString());
        Text.Anchor = TextAnchor.UpperLeft;
        if (Widgets.ButtonText(new Rect(x + 90f, rect.y + 3f, 30f, 26f), "+"))
        {
            row.selected = Mathf.Min(row.available, row.selected + step);
        }
        if (Widgets.ButtonText(new Rect(x + 124f, rect.y + 3f, 60f, 26f),
                row.selected < row.available ? "BOR_Kwazaari_All".Translate() : "BOR_Kwazaari_None".Translate()))
        {
            row.selected = row.selected < row.available ? row.available : 0;
        }
    }

    private void Sell()
    {
        float earned = 0f;
        foreach (Row row in rows)
        {
            int left = row.selected;
            foreach (Thing t in row.things)
            {
                if (left <= 0)
                {
                    break;
                }
                if (t.Destroyed || !KwazaariUtility.Sellable(t))
                {
                    continue;
                }
                int take = Mathf.Min(left, t.stackCount);
                earned += KwazaariUtility.SellAway(t, take);
                left -= take;
            }
        }
        GameComponent_Kwazaari.Instance.AddScrip(earned);
        SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
        Messages.Message("BOR_Kwazaari_Sold".Translate(KwazaariUI.Scrip(earned)), MessageTypeDefOf.PositiveEvent, false);
        Close();
    }
}

// Every favor with cost, cooldown and why it can't be bought right now.
public class Dialog_KwazaariFavors : Window
{
    private const float RowHeight = 52f;
    private const float RecheckSeconds = 1f;

    private readonly Thing console;
    private readonly Map map;
    private readonly List<KwazaariFavorDef> favors;
    private readonly Dictionary<KwazaariFavorDef, AcceptanceReport> reports = new();
    private float lastCheck = -999f;
    private Vector2 scroll;

    public Dialog_KwazaariFavors(Thing console)
    {
        this.console = console;
        map = console.Map;
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        favors = DefDatabase<KwazaariFavorDef>.AllDefs.OrderBy(d => d.order).ThenBy(d => d.cost).ToList();
    }

    public override Vector2 InitialSize => new Vector2(680f, 640f);

    public override void DoWindowContents(Rect inRect)
    {
        KwazaariUI.DrawHeader(inRect, "BOR_Kwazaari_Spend".Translate());
        // Some checks (shuttle landing spot) are costly; refresh them once a second, not every frame.
        if (Time.realtimeSinceStartup - lastCheck > RecheckSeconds)
        {
            lastCheck = Time.realtimeSinceStartup;
            foreach (KwazaariFavorDef favor in favors)
            {
                reports[favor] = GameComponent_Kwazaari.Instance.CanBuy(favor, map);
            }
        }
        Rect listRect = new Rect(inRect.x, inRect.y + 70f, inRect.width, inRect.height - 70f);
        Rect view = new Rect(0f, 0f, listRect.width - 16f, favors.Count * RowHeight);
        Widgets.BeginScrollView(listRect, ref scroll, view);
        for (int i = 0; i < favors.Count; i++)
        {
            DrawRow(new Rect(0f, i * RowHeight, view.width, RowHeight), favors[i], i);
        }
        Widgets.EndScrollView();
    }

    private void DrawRow(Rect rect, KwazaariFavorDef favor, int index)
    {
        if (index % 2 == 1)
        {
            Widgets.DrawLightHighlight(rect);
        }
        GameComponent_Kwazaari ledger = GameComponent_Kwazaari.Instance;
        AcceptanceReport report = reports[favor];

        // Favors that can't be bought are greyed out and get the reason instead of a button.
        Color tint = report.Accepted ? Color.white : Color.gray;
        Widgets.DrawTextureFitted(new Rect(rect.x + 4f, rect.y + 6f, 40f, 40f), favor.Icon, 1f, report.Accepted ? 1f : 0.4f);
        Rect info = new Rect(rect.x + 52f, rect.y, rect.width - 52f - 180f, rect.height);
        GUI.color = tint;
        Widgets.Label(new Rect(info.x, info.y + 4f, info.width, 24f), favor.LabelCap);
        GUI.color = Color.gray;
        string cooldown = "BOR_Kwazaari_FavorStats".Translate(KwazaariUI.Scrip(favor.cost), favor.cooldownDays.ToString("0.#"));
        int left = ledger.CooldownLeft(favor);
        if (left > 0)
        {
            cooldown += "  " + "BOR_Kwazaari_ReadyIn".Translate(left.ToStringTicksToPeriod());
        }
        Widgets.Label(new Rect(info.x, info.y + 26f, info.width, 24f), cooldown);
        GUI.color = Color.white;
        TooltipHandler.TipRegion(info, favor.description);

        Rect button = new Rect(rect.xMax - 176f, rect.y + 6f, 172f, 40f);
        if (!report.Accepted)
        {
            string reason = report.Reason.NullOrEmpty() ? "BOR_Kwazaari_Unavailable".Translate().ToString() : report.Reason;
            GUI.color = ColorLibrary.RedReadable;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(button, reason);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            TooltipHandler.TipRegion(button, reason);
            return;
        }
        if (Widgets.ButtonText(button.ContractedBy(0f, 5f), "BOR_Kwazaari_Request".Translate()))
        {
            Close();
            favor.Worker.Begin(map, console);
        }
    }
}
