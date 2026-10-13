//using Microsoft.AspNetCore.Html;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Services.ReportService.Table;   // Fmt
//using System.Globalization;
//using System.Text.Encodings.Web;

//namespace NexgenCosysReport.Services.ReportService.Visual
//{
//    /// <summary>
//    /// Builds any visual (chart) report:   setup  ->  title block  ->  blocks  ->  BuildAsync().
//    /// Blocks (KPIs, info cards, chart grids) are added in order and packed into pages automatically.
//    /// BuildAsync() returns the SheetReport that _SheetReport.cshtml renders (page layout, footer, "Page x of N", PDF).
//    /// </summary>
//    public sealed class VisualReport
//    {
//        // must match the values given to SheetReport below
//        private const double RowMm = 6;
//        private const double HeaderMm = 6;

//        // server-side height estimates (the browser does the real drawing)
//        private const double GapMm = 3;            // space between blocks
//        private const double KpiRowMm = 24;
//        private const double MetaLineMm = 5.5;
//        private const double DividerMm = 4;

//        private readonly Dictionary<string, object> _model;
//        private readonly string _title;
//        private readonly string _sheetName;
//        private readonly PageSizeSetting _page;

//        private string _printedTitle;
//        private string _noDataText = "";
//        private bool _noData;
//        private bool _showDivider = true;
//        private string _extraCss = "";
//        private IReadOnlyList<object> _chartRows = Array.Empty<object>();
//        private int _chartCounter;
//        private bool _breakNext;

//        private double _chartRowMm = 75;           // minimum height of one row of charts
//        private double _maxChartRowMm = 110;       // a filled chart row never grows taller than this
//        private double _firstPageExtraMm = 50;     // common header + title (meta lines and divider are added on top)
//        private double? _pageBodyMm;               // usable height of one page; default comes from PageSetting

//        private readonly List<IHtmlContent> _metaLeft = new();
//        private readonly List<IHtmlContent> _metaRight = new();
//        private readonly List<VisualChart> _charts = new();
//        private readonly List<VisualBlock> _blocks = new();

//        public VisualReport(Dictionary<string, object> model, string title, string sheetName)
//        {
//            _model = model;
//            _title = title;
//            _printedTitle = title;
//            _sheetName = sheetName;
//            _page = (model.TryGetValue("PageSetting", out var p) ? p as PageSizeSetting : null)
//                    ?? throw new InvalidOperationException("PageSetting missing in the report data.");
//        }

//        // ═════════ 1. SETUP (call these before adding blocks) ═══════════════════

//        /// <summary>The title printed on page 1, when it should differ from the report title.</summary>
//        public VisualReport PrintedTitle(string title) { _printedTitle = title; return this; }

//        /// <summary>The rows every chart is drawn from. Each chart picks a field by name (e.g. "amount").</summary>
//        public VisualReport ChartRows<T>(IEnumerable<T> rows) where T : notnull
//        {
//            _chartRows = rows.Cast<object>().ToList();
//            return this;
//        }

//        /// <summary>When the condition is true, page 1 shows the text instead of the blocks.</summary>
//        public VisualReport NoDataWhen(bool condition, string text = "No data found for the selected criteria")
//        {
//            _noData = condition;
//            _noDataText = text;
//            return this;
//        }

//        /// <summary>Removes the horizontal line under the title block.</summary>
//        public VisualReport NoDivider() { _showDivider = false; return this; }

//        /// <summary>Extra CSS for this report only (badges, special colours, .rpt-divider, .kpi ...).</summary>
//        public VisualReport Css(string css) { _extraCss += "\n" + css; return this; }

//        /// <summary>
//        /// Size estimates used to decide what fits a page. The real drawing is done in the browser.
//        /// chartRowMm = minimum height of one chart row, maxChartRowMm = tallest a filled chart row may grow,
//        /// firstPageExtraMm = common header + title on page 1, pageBodyMm = usable page height (default from PageSetting).
//        /// </summary>
//        public VisualReport Sizing(double? chartRowMm = null, double? maxChartRowMm = null,
//                                   double? firstPageExtraMm = null, double? pageBodyMm = null)
//        {
//            _chartRowMm = chartRowMm ?? _chartRowMm;
//            _maxChartRowMm = maxChartRowMm ?? _maxChartRowMm;
//            _firstPageExtraMm = firstPageExtraMm ?? _firstPageExtraMm;
//            _pageBodyMm = pageBodyMm ?? _pageBodyMm;
//            return this;
//        }

//        // ═════════ 2. TITLE BLOCK (page 1, under the title) ═════════════════════

//        public VisualReport MetaLeft(string label, string value) => MetaLeft(Fmt.Labeled(label, value));
//        public VisualReport MetaLeft(IHtmlContent line) { _metaLeft.Add(line); return this; }
//        public VisualReport MetaRight(string label, string value) => MetaRight(Fmt.Labeled(label, value));
//        public VisualReport MetaRight(IHtmlContent line) { _metaRight.Add(line); return this; }

//        // ═════════ 3. BLOCKS (added in order, packed into pages automatically) ═══
//        // Every block accepts:  style = inline CSS for the block,  cssClass = extra CSS class(es),
//        // heightMm = your own height estimate for pagination (only needed when you change the real height).

//        /// <summary>
//        /// A flex row of KPI cards (space-around, with a gap). Cards grow and shrink with the available width.
//        /// Per card: Add(..., style: "min-width:200px;height:70px"). Per row: style: "gap:16px".
//        /// </summary>
//        public VisualReport Kpis(Action<VisualKpis> build, string? style = null, string? cssClass = null, double? heightMm = null)
//        {
//            var kpis = new VisualKpis();
//            build(kpis);
//            var html = kpis.Render(style, cssClass);
//            return html is null ? this : AddBlock(heightMm ?? KpiRowMm, _ => html);
//        }

//        /// <summary>
//        /// Side-by-side info cards (small table, bar list). Cards in the row have the same height and
//        /// their rows are spread evenly. Per card: .CardStyle("...").
//        /// </summary>
//        public VisualReport InfoRow(Action<VisualInfoRow> build, string? style = null, string? cssClass = null, double? heightMm = null)
//        {
//            var row = new VisualInfoRow();
//            build(row);
//            var html = row.Render(style, cssClass);
//            return html is null ? this : AddBlock(heightMm ?? row.HeightMm, _ => html);
//        }

//        /// <summary>
//        /// A grid of chart cards (columns = 2 or 3). A long grid is split over pages automatically;
//        /// the last chart row on a page stretches to fill the remaining space.
//        /// rowMm = height of one chart row for this grid only. Per chart card: .CardStyle("...").
//        /// </summary>
//        public VisualReport ChartGrid(int columns, Action<VisualChartGrid> build,
//                                      double? rowMm = null, string? style = null, string? cssClass = null)
//        {
//            var grid = new VisualChartGrid(this, columns);
//            build(grid);
//            if (!HasChartRows) return this;                         // nothing to draw: no empty cards

//            var oneRowMm = rowMm ?? _chartRowMm;
//            var cardsPerSlice = columns == 2 ? columns * 2 : columns;   // the shared CSS has 1 or 2 rows for g2, 1 row for g3
//            foreach (var slice in grid.RenderCards().Chunk(cardsPerSlice))
//            {
//                var cards = slice;
//                var rowCount = (int)Math.Ceiling(cards.Length / (double)columns);
//                AddBlock(rowCount * oneRowMm,
//                         isLastOnPage => RenderChartSlice(columns, rowCount, oneRowMm, cards, isLastOnPage, style, cssClass));
//            }
//            return this;
//        }

//        /// <summary>Anything else you build yourself. heightMm = its estimated height, used for pagination.</summary>
//        public VisualReport Html(IHtmlContent html, double heightMm) => AddBlock(heightMm, _ => html);

//        /// <summary>The next block starts on a new page.</summary>
//        public VisualReport PageBreak() { _breakNext = true; return this; }

//        // ═════════ 4. FINISH ════════════════════════════════════════════════════

//        public async Task<SheetReport> BuildAsync(IHtmlHelper html)
//        {
//            // templates cannot await: render the shared partials once, here
//            var chartSet = new VisualChartSet { Rows = _chartRows, Charts = [.. _charts] };
//            var headerHtml = await html.PartialAsync("_CommonHeader", _model);
//            var chartsHtml = await html.PartialAsync("_VisualCharts", chartSet);

//            Func<object?, IHtmlContent> head = _ => new HtmlString("<style>" + DefaultCss + _extraCss + "</style>");
//            Func<object?, IHtmlContent> body = _ => RenderBody(headerHtml, chartsHtml);

//            return new SheetReport
//            {
//                Data = _model,
//                Title = _title,
//                SheetName = _sheetName,
//                Head = head,
//                Body = body,
//                DeferReady = true,      // the shared chart script calls window.reportReady(): it fills "Page x of N" and releases the PDF
//                RowMm = RowMm,          // keeps SheetLayout.RowMm / DetailRowsPerPage in sync with the CSS
//                HeaderMm = HeaderMm
//            };
//        }

//        // ═════════ INTERNALS ════════════════════════════════════════════════════

//        internal bool HasChartRows => _chartRows.Count > 0;
//        internal string NextChartId() => $"chart{++_chartCounter}";
//        internal void AddChart(VisualChart chart) => _charts.Add(chart);

//        private VisualReport AddBlock(double heightMm, Func<bool, IHtmlContent> render)
//        {
//            _blocks.Add(new VisualBlock { HeightMm = heightMm, Render = render, BreakBefore = _breakNext });
//            _breakNext = false;
//            return this;
//        }

//        private double PageBodyMm() => _pageBodyMm ?? _page.DetailRowsPerPage * RowMm + HeaderMm;

//        private double FirstPageBodyMm() =>
//            PageBodyMm() - _firstPageExtraMm
//            - Math.Max(_metaLeft.Count, _metaRight.Count) * MetaLineMm
//            - (_showDivider ? DividerMm : 0);

//        /// <summary>Packs the blocks into pages in order. A block that does not fit starts a new page.</summary>
//        private List<List<VisualBlock>> Paginate()
//        {
//            var pages = new List<List<VisualBlock>>();
//            var current = new List<VisualBlock>();
//            var remaining = FirstPageBodyMm();

//            foreach (var block in _blocks)
//            {
//                var needsNewPage = block.BreakBefore || block.HeightMm > remaining;
//                if (needsNewPage && current.Count > 0)
//                {
//                    pages.Add(current);
//                    current = new List<VisualBlock>();
//                    remaining = PageBodyMm();
//                }
//                current.Add(block);
//                remaining -= block.HeightMm + GapMm;
//            }

//            pages.Add(current);          // always at least one page (page 1 carries the header)
//            return pages;
//        }

//        private IHtmlContent RenderBody(IHtmlContent headerHtml, IHtmlContent chartsHtml)
//        {
//            var html = new HtmlContentBuilder();
//            var pages = _noData ? new List<List<VisualBlock>> { new() } : Paginate();

//            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
//            {
//                html.AppendHtml($"<div class=\"report-page\" data-page=\"{pageIndex + 1}\"><div class=\"sheet-body\">");

//                if (pageIndex == 0)
//                {
//                    html.AppendHtml("<div class=\"common-header-wrap\">").AppendHtml(headerHtml).AppendHtml("</div>");
//                    html.AppendHtml("<div class=\"rpt-title\">").Append(_printedTitle).AppendHtml("</div>");
//                    AppendMeta(html);
//                    if (_showDivider) html.AppendHtml("<div class=\"rpt-divider\"></div>");
//                    html.AppendHtml("<div id=\"chartError\">Chart library could not be loaded. Check network access to the Chart.js file.</div>");
//                }

//                if (_noData)
//                {
//                    html.AppendHtml("<div class=\"empty\">").Append(_noDataText).AppendHtml("</div>");
//                }
//                else
//                {
//                    var blocks = pages[pageIndex];
//                    for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
//                        html.AppendHtml(blocks[blockIndex].Render(blockIndex == blocks.Count - 1));
//                }

//                html.AppendHtml("</div><div class=\"page-footer\"></div></div>");
//            }

//            // Chart.js + the generic chart engine (draws every chart, then calls reportReady)
//            html.AppendHtml(chartsHtml);
//            return html;
//        }

//        private static IHtmlContent RenderChartSlice(int columns, int rowCount, double oneRowMm, IReadOnlyList<IHtmlContent> cards,
//                                                     bool isLastOnPage, string? userStyle, string? userClass)
//        {
//            var heightStyle = isLastOnPage
//                ? $"max-height:{Mm(rowCount * oneRowMm * 1.5)}mm"           // fills the rest of the page, up to the cap
//                : $"flex:none;height:{Mm(rowCount * oneRowMm)}mm";          // fixed height, other blocks follow it

//            var css = VisualAttr.Class($"grid g{columns}{(rowCount > 1 ? " rows2" : "")}{(isLastOnPage ? " fill" : "")}", userClass);
//            var html = new HtmlContentBuilder();
//            html.AppendHtml($"<div class=\"{css}\"{VisualAttr.Style(heightStyle + ";" + userStyle)}>");
//            foreach (var card in cards) html.AppendHtml(card);
//            html.AppendHtml("</div>");
//            return html;
//        }

//        private void AppendMeta(HtmlContentBuilder html)
//        {
//            if (_metaLeft.Count == 0 && _metaRight.Count == 0) return;

//            html.AppendHtml("<div class=\"rpt-meta\"><div>");
//            foreach (var line in _metaLeft) html.AppendHtml("<div>").AppendHtml(line).AppendHtml("</div>");
//            html.AppendHtml("</div><div class=\"text-right\">");
//            foreach (var line in _metaRight) html.AppendHtml("<div>").AppendHtml(line).AppendHtml("</div>");
//            html.AppendHtml("</div></div>");
//        }

//        internal static string Mm(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

//        // shared chart / card / grid CSS comes from _VisualCharts; this is only what the builder adds on top
//        private const string DefaultCss = @"
//            /* line under the title block */
//            .rpt-divider { flex: none; border-top: 1px solid #000; margin: 0 0 10px; }

//            /* KPI cards: flex row, spread evenly, cards grow and shrink with the space */
//            .kpi-row { flex: none; display: flex !important; flex-wrap: wrap; justify-content: space-around; align-items: stretch; gap: 10px; margin-bottom: 10px; }
//            .kpi-row .kpi { flex: 1 1 120px; min-width: 110px; max-width: 240px; box-sizing: border-box; text-align: center; }

//            /* info cards: same height side by side, rows spread evenly inside */
//            .info-row { flex: none; display: flex; align-items: stretch; gap: 10px; margin-bottom: 10px; }
//            .info-row > .card { flex: 1 1 0; min-width: 0; }
//            .card-body { flex: 1; min-height: 0; display: flex; flex-direction: column; }
//            .kv-line { flex: 1 1 auto; display: flex; justify-content: space-between; align-items: center; gap: 8px; padding: 3px 6px; border-bottom: 1px solid #eee; font-size: 9pt; }
//            .kv-line:last-child { border-bottom: 0; }
//            .kv-line .number { font-weight: bold; color: #1a3d7c; }
//            .bar-line { flex: 1 1 auto; display: flex; align-items: center; gap: 8px; font-size: 9pt; }
//            .bar-line .bar-label { flex: 0 0 38%; }
//            .bar-line .bar-track { flex: 1; background: #eef1f6; border-radius: 3px; height: 14px; }
//            .bar-line .bar-fill  { background: #2563eb; height: 14px; border-radius: 3px; }
//            .bar-line .bar-value { flex: 0 0 16%; text-align: right; font-weight: bold; color: #1a3d7c; }

//            .grid:not(.fill) { margin-bottom: 10px; }
//        ";
//    }

//    /// <summary>One block of a page: its estimated height and how to draw it (true = last block on its page).</summary>
//    internal sealed class VisualBlock
//    {
//        public required double HeightMm { get; init; }
//        public required Func<bool, IHtmlContent> Render { get; init; }
//        public bool BreakBefore { get; init; }
//    }

//    internal static class VisualAttr
//    {
//        public static string Class(string baseClass, string? extra) =>
//            HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(extra) ? baseClass : baseClass + " " + extra.Trim());

//        public static string Style(string? style) =>
//            string.IsNullOrWhiteSpace(style) ? "" : $" style=\"{HtmlEncoder.Default.Encode(style.Trim(';', ' ') + ";")}\"";
//    }

//    // ───────────────────────────── KPI cards ─────────────────────────────

//    public sealed class VisualKpis
//    {
//        private readonly HtmlContentBuilder _cards = new();
//        private int _count;

//        /// <summary>
//        /// One KPI card. smallValue = smaller font, for long text such as a name.
//        /// style = inline CSS for this card, e.g. "min-width:200px;height:70px" (it can also grow or shrink with flex).
//        /// </summary>
//        public VisualKpis Add(string label, string value, string note = "", bool smallValue = false, string? style = null)
//        {
//            _count++;
//            _cards.AppendHtml($"<div class=\"kpi\"{VisualAttr.Style(style)}><div class=\"l\">").Append(label).AppendHtml("</div>");
//            _cards.AppendHtml(smallValue ? "<div class=\"v\" style=\"font-size:12pt\">" : "<div class=\"v\">")
//                  .Append(value).AppendHtml("</div>");
//            _cards.AppendHtml("<div class=\"s\">").Append(note).AppendHtml("</div></div>");
//            return this;
//        }

//        internal IHtmlContent? Render(string? style, string? cssClass)
//        {
//            if (_count == 0) return null;
//            var html = new HtmlContentBuilder();
//            html.AppendHtml($"<div class=\"{VisualAttr.Class("kpi-row", cssClass)}\"{VisualAttr.Style(style)}>")
//                .AppendHtml(_cards).AppendHtml("</div>");
//            return html;
//        }
//    }

//    // ───────────────────────────── info cards ─────────────────────────────

//    public sealed class VisualInfoRow
//    {
//        private const double CardTitleMm = 14;     // title + card padding
//        private const double LineMm = 7;

//        private sealed class InfoCard
//        {
//            public required string Title { get; init; }
//            public required HtmlContentBuilder Body { get; init; }
//            public required double HeightMm { get; init; }
//            public string? Style { get; set; }
//        }

//        private readonly List<InfoCard> _cards = new();

//        /// <summary>Estimated height of the row = its tallest card (used for pagination).</summary>
//        internal double HeightMm => _cards.Count == 0 ? 0 : _cards.Max(card => card.HeightMm);

//        /// <summary>A card with a label / value list. Skipped when there are no items.</summary>
//        public VisualInfoRow Table(string title, IEnumerable<(string Label, string Value)> items)
//        {
//            var list = items.ToList();
//            if (list.Count == 0) return this;

//            var body = new HtmlContentBuilder();
//            foreach (var (label, value) in list)
//                body.AppendHtml("<div class=\"kv-line\"><span>").Append(label).AppendHtml("</span><span class=\"number\">")
//                    .Append(value).AppendHtml("</span></div>");

//            _cards.Add(new InfoCard { Title = title, Body = body, HeightMm = CardTitleMm + list.Count * LineMm });
//            return this;
//        }

//        /// <summary>A card with one horizontal bar per item, scaled to the largest value. Skipped when there are no items.</summary>
//        public VisualInfoRow BarList(string title, IEnumerable<(string Label, decimal Value)> items, string valueFormat = "N0")
//        {
//            var list = items.ToList();
//            if (list.Count == 0) return this;

//            var maximum = list.Max(item => item.Value);
//            var body = new HtmlContentBuilder();
//            foreach (var (label, value) in list)
//            {
//                var width = maximum <= 0 ? 0 : (double)(value / maximum * 100);
//                body.AppendHtml("<div class=\"bar-line\"><div class=\"bar-label\">").Append(label).AppendHtml("</div>")
//                    .AppendHtml($"<div class=\"bar-track\"><div class=\"bar-fill\" style=\"width:{VisualReport.Mm(width)}%\"></div></div>")
//                    .AppendHtml("<div class=\"bar-value\">").Append(value.ToString(valueFormat)).AppendHtml("</div></div>");
//            }

//            _cards.Add(new InfoCard { Title = title, Body = body, HeightMm = CardTitleMm + list.Count * LineMm });
//            return this;
//        }

//        /// <summary>Inline CSS for the card added last, e.g. "flex:2 1 0" to make it wider.</summary>
//        public VisualInfoRow CardStyle(string css)
//        {
//            if (_cards.Count > 0) _cards[^1].Style = css;
//            return this;
//        }

//        internal IHtmlContent? Render(string? style, string? cssClass)
//        {
//            if (_cards.Count == 0) return null;
//            var html = new HtmlContentBuilder();
//            html.AppendHtml($"<div class=\"{VisualAttr.Class("info-row", cssClass)}\"{VisualAttr.Style(style)}>");
//            foreach (var card in _cards)
//            {
//                html.AppendHtml($"<div class=\"card\"{VisualAttr.Style(card.Style)}><h3>").Append(card.Title)
//                    .AppendHtml("</h3><div class=\"card-body\">").AppendHtml(card.Body).AppendHtml("</div></div>");
//            }
//            html.AppendHtml("</div>");
//            return html;
//        }
//    }

//    // ───────────────────────────── chart grid ─────────────────────────────

//    public sealed class VisualChartGrid
//    {
//        private sealed class ChartCard
//        {
//            public required string Title { get; init; }
//            public required string Id { get; init; }
//            public string? Style { get; set; }
//        }

//        private readonly VisualReport _report;
//        private readonly List<ChartCard> _cards = new();

//        internal VisualChartGrid(VisualReport report, int columns)
//        {
//            if (columns is not (2 or 3))
//                throw new InvalidOperationException("ChartGrid supports 2 or 3 columns.");
//            _report = report;
//        }

//        /// <summary>Doughnut of one field. money = show values as amounts.</summary>
//        public VisualChartGrid Donut(string title, string field, bool money = false)
//            => Add(title, id => VisualChart.Donut(id, field, money: money));

//        /// <summary>One bar per row for one field.</summary>
//        public VisualChartGrid Bar(string title, string field, string color, bool horizontal = true, int? thickness = null)
//            => Add(title, id => VisualChart.Bar(id, field, color, horizontal: horizontal, thickness: thickness));

//        /// <summary>Several series side by side for each row, e.g. Amount vs Provision Amount.</summary>
//        public VisualChartGrid Grouped(string title, params VisualSeries[] series)
//            => Add(title, id => VisualChart.Grouped(id, series));

//        /// <summary>Bars with a line on top (e.g. amount bars and a percentage line). format: "percent" for percentages.</summary>
//        public VisualChartGrid BarLine(string title, string field, string barColor, string lineColor, string format = "compact")
//            => Add(title, id => VisualChart.BarLine(id, field, barColor, lineColor, format));

//        /// <summary>Inline CSS for the chart card added last, e.g. "grid-column: span 2".</summary>
//        public VisualChartGrid CardStyle(string css)
//        {
//            if (_cards.Count > 0) _cards[^1].Style = css;
//            return this;
//        }

//        private VisualChartGrid Add(string title, Func<string, VisualChart> createChart)
//        {
//            var id = _report.NextChartId();      // ids are generated, you never type them
//            _report.AddChart(createChart(id));
//            _cards.Add(new ChartCard { Title = title, Id = id });
//            return this;
//        }

//        internal IReadOnlyList<IHtmlContent> RenderCards() => _cards.Select(card =>
//        {
//            var html = new HtmlContentBuilder();
//            html.AppendHtml($"<div class=\"card\"{VisualAttr.Style(card.Style)}><h3>").Append(card.Title).AppendHtml("</h3>")
//                .AppendHtml($"<div class=\"chart-box\"><canvas id=\"{card.Id}\"></canvas></div></div>");
//            return (IHtmlContent)html;
//        }).ToList();
//    }
//}







using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Services.ReportService.Table;   // Fmt
using System.Globalization;
using System.Text.Encodings.Web;

namespace NexgenCosysReport.Services.ReportService.Visual
{
    /// <summary>
    /// Builds any visual (chart) report:   setup  ->  title block  ->  blocks  ->  BuildAsync().
    /// Blocks (KPIs, info cards, chart grids) are added in order and packed into pages automatically.
    /// Chart grids fill the free page height and KPI cards fill the row width by default.
    /// BuildAsync() returns the SheetReport that _SheetReport.cshtml renders (page layout, footer, "Page x of N", PDF).
    /// </summary>
    public sealed class VisualReport
    {
        // must match the values given to SheetReport below
        private const double RowMm = 6;
        private const double HeaderMm = 6;

        // server-side height estimates (the browser does the real drawing)
        private const double GapMm = 3;            // space between blocks
        private const double KpiRowMm = 24;
        private const double MetaLineMm = 5.5;
        private const double DividerMm = 4;

        private readonly Dictionary<string, object> _model;
        private readonly string _title;
        private readonly string _sheetName;
        private readonly PageSizeSetting _page;

        private string _printedTitle;
        private string _noDataText = "";
        private bool _noData;
        private bool _showDivider = true;
        private string _extraCss = "";
        private IReadOnlyList<object> _chartRows = Array.Empty<object>();
        private int _chartCounter;
        private bool _breakNext;

        private double _chartRowMm = 75;           // minimum height of one row of charts
        private double? _maxChartRowMm;            // null = no cap: a filled chart row takes all the free height
        private double _firstPageExtraMm = 50;     // common header + title (meta lines and divider are added on top)
        private double? _pageBodyMm;               // usable height of one page; default comes from PageSetting

        private readonly List<IHtmlContent> _metaLeft = new();
        private readonly List<IHtmlContent> _metaRight = new();
        private readonly List<VisualChart> _charts = new();
        private readonly List<VisualBlock> _blocks = new();

        public VisualReport(Dictionary<string, object> model, string title, string sheetName)
        {
            _model = model;
            _title = title;
            _printedTitle = title;
            _sheetName = sheetName;
            _page = (model.TryGetValue("PageSetting", out var p) ? p as PageSizeSetting : null)
                    ?? throw new InvalidOperationException("PageSetting missing in the report data.");
        }

        // ═════════ 1. SETUP (call these before adding blocks) ═══════════════════

        /// <summary>The title printed on page 1, when it should differ from the report title.</summary>
        public VisualReport PrintedTitle(string title) { _printedTitle = title; return this; }

        /// <summary>The rows every chart is drawn from. Each chart picks a field by name (e.g. "amount").</summary>
        public VisualReport ChartRows<T>(IEnumerable<T> rows) where T : notnull
        {
            _chartRows = rows.Cast<object>().ToList();
            return this;
        }

        /// <summary>When the condition is true, page 1 shows the text instead of the blocks.</summary>
        public VisualReport NoDataWhen(bool condition, string text = "No data found for the selected criteria")
        {
            _noData = condition;
            _noDataText = text;
            return this;
        }

        /// <summary>Removes the horizontal line under the title block.</summary>
        public VisualReport NoDivider() { _showDivider = false; return this; }

        /// <summary>Extra CSS for this report only (badges, special colours, .rpt-divider, .kpi ...).</summary>
        public VisualReport Css(string css) { _extraCss += "\n" + css; return this; }

        /// <summary>
        /// Size estimates used to decide what fits a page. The real drawing is done in the browser.
        /// chartRowMm = minimum height of one chart row, maxChartRowMm = cap for a filled chart row (default: no cap),
        /// firstPageExtraMm = common header + title on page 1, pageBodyMm = usable page height (default from PageSetting).
        /// </summary>
        public VisualReport Sizing(double? chartRowMm = null, double? maxChartRowMm = null,
                                   double? firstPageExtraMm = null, double? pageBodyMm = null)
        {
            _chartRowMm = chartRowMm ?? _chartRowMm;
            _maxChartRowMm = maxChartRowMm ?? _maxChartRowMm;
            _firstPageExtraMm = firstPageExtraMm ?? _firstPageExtraMm;
            _pageBodyMm = pageBodyMm ?? _pageBodyMm;
            return this;
        }

        // ═════════ 2. TITLE BLOCK (page 1, under the title) ═════════════════════

        public VisualReport MetaLeft(string label, string value) => MetaLeft(Fmt.Labeled(label, value));
        public VisualReport MetaLeft(IHtmlContent line) { _metaLeft.Add(line); return this; }
        public VisualReport MetaRight(string label, string value) => MetaRight(Fmt.Labeled(label, value));
        public VisualReport MetaRight(IHtmlContent line) { _metaRight.Add(line); return this; }

        // ═════════ 3. BLOCKS (added in order, packed into pages automatically) ═══
        // Every block accepts:  style = inline CSS for the block,  cssClass = extra CSS class(es),
        // heightMm = your own height estimate for pagination (only needed when you change the real height).

        /// <summary>
        /// A flex row of KPI cards (space-around, with a gap). By default the cards share the full row width.
        /// Per card: Add(..., style: "max-width:200px;height:70px"). Per row: style: "gap:16px".
        /// </summary>
        public VisualReport Kpis(Action<VisualKpis> build, string? style = null, string? cssClass = null, double? heightMm = null)
        {
            var kpis = new VisualKpis();
            build(kpis);
            var html = kpis.Render(style, cssClass);
            return html is null ? this : AddBlock(heightMm ?? KpiRowMm, _ => html);
        }

        /// <summary>
        /// Side-by-side info cards (small table, bar list). Cards in the row have the same height and their
        /// rows are spread evenly. fill = true stretches the row to the free page height (default: content height).
        /// Per card: .CardStyle("...").
        /// </summary>
        public VisualReport InfoRow(Action<VisualInfoRow> build, string? style = null, string? cssClass = null,
                                    double? heightMm = null, bool fill = false)
        {
            var row = new VisualInfoRow();
            build(row);
            var html = row.Render(fill ? "flex:1 1 0;" + style : style, cssClass);
            return html is null ? this : AddBlock(heightMm ?? row.HeightMm, _ => html);
        }

        /// <summary>
        /// A grid of chart cards (columns = 2 or 3). A long grid is split over pages automatically.
        /// By default (fill = true) the grid fills the free page height; several filled grids on one page share it.
        /// fill = false gives a fixed height of rowMm per chart row. rowMm = minimum (or fixed) height of one chart row.
        /// Per chart card: .CardStyle("...").
        /// </summary>
        public VisualReport ChartGrid(int columns, Action<VisualChartGrid> build,
                                      double? rowMm = null, string? style = null, string? cssClass = null,
                                      bool fill = true)
        {
            var grid = new VisualChartGrid(this, columns);
            build(grid);
            if (!HasChartRows) return this;                         // nothing to draw: no empty cards

            var oneRowMm = rowMm ?? _chartRowMm;
            var cardsPerSlice = columns == 2 ? columns * 2 : columns;   // the shared CSS has 1 or 2 rows for g2, 1 row for g3
            foreach (var slice in grid.RenderCards().Chunk(cardsPerSlice))
            {
                var cards = slice;
                var rowCount = (int)Math.Ceiling(cards.Length / (double)columns);
                AddBlock(rowCount * oneRowMm,
                         _ => RenderChartSlice(columns, rowCount, oneRowMm, cards, fill, style, cssClass));
            }
            return this;
        }

        /// <summary>Anything else you build yourself. heightMm = its estimated height, used for pagination.</summary>
        public VisualReport Html(IHtmlContent html, double heightMm) => AddBlock(heightMm, _ => html);

        /// <summary>The next block starts on a new page.</summary>
        public VisualReport PageBreak() { _breakNext = true; return this; }

        // ═════════ 4. FINISH ════════════════════════════════════════════════════

        public async Task<SheetReport> BuildAsync(IHtmlHelper html)
        {
            // templates cannot await: render the shared partials once, here
            var chartSet = new VisualChartSet { Rows = _chartRows, Charts = [.. _charts] };
            var headerHtml = await html.PartialAsync("_CommonHeader", _model);
            var chartsHtml = await html.PartialAsync("_VisualCharts", chartSet);

            Func<object?, IHtmlContent> head = _ => new HtmlString("<style>" + DefaultCss + _extraCss + "</style>");
            Func<object?, IHtmlContent> body = _ => RenderBody(headerHtml, chartsHtml);

            return new SheetReport
            {
                Data = _model,
                Title = _title,
                SheetName = _sheetName,
                Head = head,
                Body = body,
                DeferReady = true,      // the shared chart script calls window.reportReady(): it fills "Page x of N" and releases the PDF
                RowMm = RowMm,          // keeps SheetLayout.RowMm / DetailRowsPerPage in sync with the CSS
                HeaderMm = HeaderMm
            };
        }

        // ═════════ INTERNALS ════════════════════════════════════════════════════

        internal bool HasChartRows => _chartRows.Count > 0;
        internal string NextChartId() => $"chart{++_chartCounter}";
        internal void AddChart(VisualChart chart) => _charts.Add(chart);

        private VisualReport AddBlock(double heightMm, Func<bool, IHtmlContent> render)
        {
            _blocks.Add(new VisualBlock { HeightMm = heightMm, Render = render, BreakBefore = _breakNext });
            _breakNext = false;
            return this;
        }

        private double PageBodyMm() => _pageBodyMm ?? _page.DetailRowsPerPage * RowMm + HeaderMm;

        private double FirstPageBodyMm() =>
            PageBodyMm() - _firstPageExtraMm
            - Math.Max(_metaLeft.Count, _metaRight.Count) * MetaLineMm
            - (_showDivider ? DividerMm : 0);

        /// <summary>Packs the blocks into pages in order. A block that does not fit starts a new page.</summary>
        private List<List<VisualBlock>> Paginate()
        {
            var pages = new List<List<VisualBlock>>();
            var current = new List<VisualBlock>();
            var remaining = FirstPageBodyMm();

            foreach (var block in _blocks)
            {
                var needsNewPage = block.BreakBefore || block.HeightMm > remaining;
                if (needsNewPage && current.Count > 0)
                {
                    pages.Add(current);
                    current = new List<VisualBlock>();
                    remaining = PageBodyMm();
                }
                current.Add(block);
                remaining -= block.HeightMm + GapMm;
            }

            pages.Add(current);          // always at least one page (page 1 carries the header)
            return pages;
        }

        private IHtmlContent RenderBody(IHtmlContent headerHtml, IHtmlContent chartsHtml)
        {
            var html = new HtmlContentBuilder();
            var pages = _noData ? new List<List<VisualBlock>> { new() } : Paginate();

            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                html.AppendHtml($"<div class=\"report-page\" data-page=\"{pageIndex + 1}\"><div class=\"sheet-body\">");

                if (pageIndex == 0)
                {
                    html.AppendHtml("<div class=\"common-header-wrap\">").AppendHtml(headerHtml).AppendHtml("</div>");
                    html.AppendHtml("<div class=\"rpt-title\">").Append(_printedTitle).AppendHtml("</div>");
                    AppendMeta(html);
                    if (_showDivider) html.AppendHtml("<div class=\"rpt-divider\"></div>");
                    html.AppendHtml("<div id=\"chartError\">Chart library could not be loaded. Check network access to the Chart.js file.</div>");
                }

                if (_noData)
                {
                    html.AppendHtml("<div class=\"empty\">").Append(_noDataText).AppendHtml("</div>");
                }
                else
                {
                    var blocks = pages[pageIndex];
                    for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
                        html.AppendHtml(blocks[blockIndex].Render(blockIndex == blocks.Count - 1));
                }

                html.AppendHtml("</div><div class=\"page-footer\"></div></div>");
            }

            // Chart.js + the generic chart engine (draws every chart, then calls reportReady)
            html.AppendHtml(chartsHtml);
            return html;
        }

        private IHtmlContent RenderChartSlice(int columns, int rowCount, double oneRowMm, IReadOnlyList<IHtmlContent> cards,
                                              bool fill, string? userStyle, string? userClass)
        {
            var minHeight = Mm(rowCount * oneRowMm);
            var cap = _maxChartRowMm is null ? "" : $";max-height:{Mm(rowCount * _maxChartRowMm.Value)}mm";

            // fill: share the free height (more chart rows get a bigger share), never smaller than the minimum
            var heightStyle = fill
                ? $"flex:{rowCount} 1 0;min-height:{minHeight}mm{cap}"
                : $"flex:none;height:{minHeight}mm";

            var css = VisualAttr.Class($"grid g{columns}{(rowCount > 1 ? " rows2" : "")}{(fill ? " fill" : "")}", userClass);
            var html = new HtmlContentBuilder();
            html.AppendHtml($"<div class=\"{css}\"{VisualAttr.Style(heightStyle + ";" + userStyle)}>");
            foreach (var card in cards) html.AppendHtml(card);
            html.AppendHtml("</div>");
            return html;
        }

        private void AppendMeta(HtmlContentBuilder html)
        {
            if (_metaLeft.Count == 0 && _metaRight.Count == 0) return;

            html.AppendHtml("<div class=\"rpt-meta\"><div>");
            foreach (var line in _metaLeft) html.AppendHtml("<div>").AppendHtml(line).AppendHtml("</div>");
            html.AppendHtml("</div><div class=\"text-right\">");
            foreach (var line in _metaRight) html.AppendHtml("<div>").AppendHtml(line).AppendHtml("</div>");
            html.AppendHtml("</div></div>");
        }

        internal static string Mm(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        // shared chart / card / grid CSS comes from _VisualCharts; this is only what the builder adds on top
        private const string DefaultCss = @"
            /* the page body is a flex column, so blocks can share the free height */
            .report-page .sheet-body { display: flex; flex-direction: column; }

            /* line under the title block */
            .rpt-divider { flex: none; border-top: 1px solid #000; margin: 0 0 10px; }

            /* KPI cards: flex row with a gap, the cards share the full row width */
            .kpi-row { flex: none; display: flex !important; flex-wrap: wrap; justify-content: space-around; align-items: stretch; gap: 10px; margin-bottom: 10px; }
            .kpi-row .kpi { flex: 1 1 120px; min-width: 110px; box-sizing: border-box; text-align: center; }

            /* info cards: same height side by side, rows spread evenly inside */
            .info-row { flex: none; display: flex; align-items: stretch; gap: 10px; margin-bottom: 10px; }
            .info-row > .card { flex: 1 1 0; min-width: 0; }
            .card-body { flex: 1; min-height: 0; display: flex; flex-direction: column; }
            .kv-line { flex: 1 1 auto; display: flex; justify-content: space-between; align-items: center; gap: 8px; padding: 3px 6px; border-bottom: 1px solid #eee; font-size: 9pt; }
            .kv-line:last-child { border-bottom: 0; }
            .kv-line .number { font-weight: bold; color: #1a3d7c; }
            .bar-line { flex: 1 1 auto; display: flex; align-items: center; gap: 8px; font-size: 9pt; }
            .bar-line .bar-label { flex: 0 0 38%; }
            .bar-line .bar-track { flex: 1; background: #eef1f6; border-radius: 3px; height: 14px; }
            .bar-line .bar-fill  { background: #2563eb; height: 14px; border-radius: 3px; }
            .bar-line .bar-value { flex: 0 0 16%; text-align: right; font-weight: bold; color: #1a3d7c; }

            /* chart grids: every row stretches, so a one-row grid fills its height too */
            .grid { margin-bottom: 10px; }
            .grid.fill { grid-auto-rows: minmax(0, 1fr); }
            .sheet-body > .grid:last-child { margin-bottom: 0; }
        ";
    }

    /// <summary>One block of a page: its estimated height and how to draw it (true = last block on its page).</summary>
    internal sealed class VisualBlock
    {
        public required double HeightMm { get; init; }
        public required Func<bool, IHtmlContent> Render { get; init; }
        public bool BreakBefore { get; init; }
    }

    internal static class VisualAttr
    {
        public static string Class(string baseClass, string? extra) =>
            HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(extra) ? baseClass : baseClass + " " + extra.Trim());

        public static string Style(string? style) =>
            string.IsNullOrWhiteSpace(style) ? "" : $" style=\"{HtmlEncoder.Default.Encode(style.Trim(';', ' ') + ";")}\"";
    }

    // ───────────────────────────── KPI cards ─────────────────────────────

    public sealed class VisualKpis
    {
        private readonly HtmlContentBuilder _cards = new();
        private int _count;

        /// <summary>
        /// One KPI card. smallValue = smaller font, for long text such as a name.
        /// style = inline CSS for this card, e.g. "max-width:200px;height:70px" (or "flex:2 1 160px" to make it wider).
        /// </summary>
        public VisualKpis Add(string label, string value, string note = "", bool smallValue = false, string? style = null)
        {
            _count++;
            _cards.AppendHtml($"<div class=\"kpi\"{VisualAttr.Style(style)}><div class=\"l\">").Append(label).AppendHtml("</div>");
            _cards.AppendHtml(smallValue ? "<div class=\"v\" style=\"font-size:12pt\">" : "<div class=\"v\">")
                  .Append(value).AppendHtml("</div>");
            _cards.AppendHtml("<div class=\"s\">").Append(note).AppendHtml("</div></div>");
            return this;
        }

        internal IHtmlContent? Render(string? style, string? cssClass)
        {
            if (_count == 0) return null;
            var html = new HtmlContentBuilder();
            html.AppendHtml($"<div class=\"{VisualAttr.Class("kpi-row", cssClass)}\"{VisualAttr.Style(style)}>")
                .AppendHtml(_cards).AppendHtml("</div>");
            return html;
        }
    }

    // ───────────────────────────── info cards ─────────────────────────────

    public sealed class VisualInfoRow
    {
        private const double CardTitleMm = 14;     // title + card padding
        private const double LineMm = 7;

        private sealed class InfoCard
        {
            public required string Title { get; init; }
            public required HtmlContentBuilder Body { get; init; }
            public required double HeightMm { get; init; }
            public string? Style { get; set; }
        }

        private readonly List<InfoCard> _cards = new();

        /// <summary>Estimated height of the row = its tallest card (used for pagination).</summary>
        internal double HeightMm => _cards.Count == 0 ? 0 : _cards.Max(card => card.HeightMm);

        /// <summary>A card with a label / value list. Skipped when there are no items.</summary>
        public VisualInfoRow Table(string title, IEnumerable<(string Label, string Value)> items)
        {
            var list = items.ToList();
            if (list.Count == 0) return this;

            var body = new HtmlContentBuilder();
            foreach (var (label, value) in list)
                body.AppendHtml("<div class=\"kv-line\"><span>").Append(label).AppendHtml("</span><span class=\"number\">")
                    .Append(value).AppendHtml("</span></div>");

            _cards.Add(new InfoCard { Title = title, Body = body, HeightMm = CardTitleMm + list.Count * LineMm });
            return this;
        }

        /// <summary>A card with one horizontal bar per item, scaled to the largest value. Skipped when there are no items.</summary>
        public VisualInfoRow BarList(string title, IEnumerable<(string Label, decimal Value)> items, string valueFormat = "N0")
        {
            var list = items.ToList();
            if (list.Count == 0) return this;

            var maximum = list.Max(item => item.Value);
            var body = new HtmlContentBuilder();
            foreach (var (label, value) in list)
            {
                var width = maximum <= 0 ? 0 : (double)(value / maximum * 100);
                body.AppendHtml("<div class=\"bar-line\"><div class=\"bar-label\">").Append(label).AppendHtml("</div>")
                    .AppendHtml($"<div class=\"bar-track\"><div class=\"bar-fill\" style=\"width:{VisualReport.Mm(width)}%\"></div></div>")
                    .AppendHtml("<div class=\"bar-value\">").Append(value.ToString(valueFormat)).AppendHtml("</div></div>");
            }

            _cards.Add(new InfoCard { Title = title, Body = body, HeightMm = CardTitleMm + list.Count * LineMm });
            return this;
        }

        /// <summary>Inline CSS for the card added last, e.g. "flex:2 1 0" to make it wider.</summary>
        public VisualInfoRow CardStyle(string css)
        {
            if (_cards.Count > 0) _cards[^1].Style = css;
            return this;
        }

        internal IHtmlContent? Render(string? style, string? cssClass)
        {
            if (_cards.Count == 0) return null;
            var html = new HtmlContentBuilder();
            html.AppendHtml($"<div class=\"{VisualAttr.Class("info-row", cssClass)}\"{VisualAttr.Style(style)}>");
            foreach (var card in _cards)
            {
                html.AppendHtml($"<div class=\"card\"{VisualAttr.Style(card.Style)}><h3>").Append(card.Title)
                    .AppendHtml("</h3><div class=\"card-body\">").AppendHtml(card.Body).AppendHtml("</div></div>");
            }
            html.AppendHtml("</div>");
            return html;
        }
    }

    // ───────────────────────────── chart grid ─────────────────────────────

    public sealed class VisualChartGrid
    {
        private sealed class ChartCard
        {
            public required string Title { get; init; }
            public required string Id { get; init; }
            public string? Style { get; set; }
        }

        private readonly VisualReport _report;
        private readonly List<ChartCard> _cards = new();

        internal VisualChartGrid(VisualReport report, int columns)
        {
            if (columns is not (2 or 3))
                throw new InvalidOperationException("ChartGrid supports 2 or 3 columns.");
            _report = report;
        }

        /// <summary>Doughnut of one field. money = show values as amounts.</summary>
        public VisualChartGrid Donut(string title, string field, bool money = false)
            => Add(title, id => VisualChart.Donut(id, field, money: money));

        /// <summary>One bar per row for one field.</summary>
        public VisualChartGrid Bar(string title, string field, string color, bool horizontal = true, int? thickness = null)
            => Add(title, id => VisualChart.Bar(id, field, color, horizontal: horizontal, thickness: thickness));

        /// <summary>Several series side by side for each row, e.g. Amount vs Provision Amount.</summary>
        public VisualChartGrid Grouped(string title, params VisualSeries[] series)
            => Add(title, id => VisualChart.Grouped(id, series));

        /// <summary>Bars with a line on top (e.g. amount bars and a percentage line). format: "percent" for percentages.</summary>
        public VisualChartGrid BarLine(string title, string field, string barColor, string lineColor, string format = "compact")
            => Add(title, id => VisualChart.BarLine(id, field, barColor, lineColor, format));

        /// <summary>Inline CSS for the chart card added last, e.g. "grid-column: span 2".</summary>
        public VisualChartGrid CardStyle(string css)
        {
            if (_cards.Count > 0) _cards[^1].Style = css;
            return this;
        }

        private VisualChartGrid Add(string title, Func<string, VisualChart> createChart)
        {
            var id = _report.NextChartId();      // ids are generated, you never type them
            _report.AddChart(createChart(id));
            _cards.Add(new ChartCard { Title = title, Id = id });
            return this;
        }

        internal IReadOnlyList<IHtmlContent> RenderCards() => _cards.Select(card =>
        {
            var html = new HtmlContentBuilder();
            html.AppendHtml($"<div class=\"card\"{VisualAttr.Style(card.Style)}><h3>").Append(card.Title).AppendHtml("</h3>")
                .AppendHtml($"<div class=\"chart-box\"><canvas id=\"{card.Id}\"></canvas></div></div>");
            return (IHtmlContent)html;
        }).ToList();
    }
}