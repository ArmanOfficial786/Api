using Microsoft.AspNetCore.Html;
using NexgenCosysReport.Dtos.ReportDtos;
using System.Text.Encodings.Web;

namespace NexgenCosysReport.Services.ReportService.Table
{
    /// <summary>
    /// Builds any tabular report:   columns  ->  title block  ->  rows  ->  Build().
    /// Build() returns the SheetReport that _SheetReport.cshtml renders (pagination, footer, PDF, Excel).
    /// </summary>
    public sealed class ReportTable
    {
        private readonly Dictionary<string, object> _model;
        private readonly string _title;
        private readonly string _sheetName;
        private readonly PageSizeSetting _page;

        private readonly List<ReportColumn> _columns = new();
        private readonly List<ReportRow> _rows = new();
        private readonly List<Func<int, IHtmlContent>> _titleBlock = new();   // each line gets the column count

        private ReportStyle _style = new();
        private string _extraCss = "";
        private bool _showColumnHeader = true;
        private double _rowMm = 7;
        private double _headerMm = 8;
        private double _firstPageExtraMm = 70;
        private int _stripe;                                                  // data rows since the last section / group / header

        public ReportTable(Dictionary<string, object> model, string title, string sheetName)
        {
            _model = model;
            _title = title;
            _sheetName = sheetName;
            _page = (model.TryGetValue("PageSetting", out var p) ? p as PageSizeSetting : null)
                    ?? throw new InvalidOperationException("PageSetting missing in the report data.");

            // the printed title is always the first line of the title block
            _titleBlock.Add(n => Raw($"<tr class=\"meta-title\"><td colspan=\"{n}\">{E(_title)}</td></tr>"));
        }

        // ═════════ 1. SETUP ═════════════════════════════════════════════════════

        /// <summary>Adds a column. widthPercent is a share of the printable width (they do not need to sum to 100).</summary>
        public ReportTable Column(string title, double widthPercent, Align alignment = Align.Left)
        {
            _columns.Add(new ReportColumn(title, widthPercent, alignment));
            return this;
        }

        /// <summary>Hide the column titles printed under the title block (use Header() rows instead).</summary>
        public ReportTable NoColumnHeader() { _showColumnHeader = false; return this; }

        public ReportTable Style(ReportStyle style) { _style = style; return this; }

        /// <summary>Extra CSS for this report only (badges, special colours ...).</summary>
        public ReportTable Css(string css) { _extraCss += "\n" + css; return this; }

        /// <summary>Server-side size estimates used to decide how many rows fit a page. The real fit is measured in the browser.</summary>
        public ReportTable Sizing(double? rowMm = null, double? headerMm = null, double? firstPageExtraMm = null)
        {
            _rowMm = rowMm ?? _rowMm;
            _headerMm = headerMm ?? _headerMm;
            _firstPageExtraMm = firstPageExtraMm ?? _firstPageExtraMm;
            return this;
        }

        // ═════════ 2. TITLE BLOCK (printed once, on page 1) ═════════════════════

        /// <summary>"Label: value" on its own line. The label sits in the first column, the value spans the rest.</summary>
        public ReportTable Meta(string label, string value)
        {
            _titleBlock.Add(n => Raw(
                $"<tr class=\"meta-row\"><td class=\"meta-label\"><strong>{E(label)}</strong></td>" +
                $"<td colspan=\"{Math.Max(1, n - 1)}\">{E(value)}</td></tr>"));
            return this;
        }

        /// <summary>Two "label value" items on one line: one left, one right-aligned.</summary>
        public ReportTable MetaPair(string leftLabel, string leftValue, string rightLabel, string rightValue)
            => MetaRow(Fmt.Labeled(leftLabel, leftValue), Fmt.Labeled(rightLabel, rightValue));

        /// <summary>A line with free content (badges, optional parts). Use Fmt.Labeled / Fmt.Badge / Fmt.Join to build it.</summary>
        public ReportTable MetaRow(IHtmlContent left, IHtmlContent? right = null, int? leftSpan = null)
        {
            _titleBlock.Add(n =>
            {
                var html = new HtmlContentBuilder();
                html.AppendHtml("<tr class=\"meta-row\">");
                if (right is null)
                {
                    html.AppendHtml($"<td colspan=\"{n}\">").AppendHtml(left).AppendHtml("</td>");
                }
                else
                {
                    var l = Math.Clamp(leftSpan ?? (n + 1) / 2, 1, Math.Max(1, n - 1));
                    html.AppendHtml($"<td colspan=\"{l}\">").AppendHtml(left).AppendHtml("</td>")
                        .AppendHtml($"<td colspan=\"{Math.Max(1, n - l)}\" class=\"text-right\">").AppendHtml(right).AppendHtml("</td>");
                }
                html.AppendHtml("</tr>");
                return html;
            });
            return this;
        }

        // ═════════ 3. ROWS ══════════════════════════════════════════════════════

        public ReportTable Section(string title)
            => Add(RowKind.Section, new ReportCell(title, Align.Left, ColumnCount()));

        /// <summary>Full-width bold line that starts a group (title only, no values).</summary>
        public ReportTable GroupTitle(string title)
            => Add(RowKind.Group, new ReportCell(title, Align.Left, ColumnCount()));

        /// <summary>Group line with its own cells (title plus group totals). Cells must cover all columns.</summary>
        public ReportTable GroupBy(params ReportCell[] cells)
            => Add(RowKind.Group, cells);

        public ReportTable Note(string text)
            => Add(RowKind.Note, new ReportCell(text, Align.Left, ColumnCount()));

        public ReportTable Spacer()
            => Add(RowKind.Spacer, new ReportCell("", Align.Left, ColumnCount()));

        /// <summary>Column titles in the middle of the table (same titles as the columns).</summary>
        public ReportTable Header()
            => Add(RowKind.Header, _columns.Select(c => new ReportCell(c.Title, c.Alignment)).ToArray());

        /// <summary>Column titles in the middle of the table (different titles; alignment follows the column).</summary>
        public ReportTable Header(params string[] titles)
            => Add(RowKind.Header, CellsFor(titles));

        /// <summary>A data row: one value per column, aligned like its column.</summary>
        public ReportTable Data(params string[] values)
            => Add(RowKind.Data, CellsFor(values));

        /// <summary>A data row with full control of every cell (alignment, span, css).</summary>
        public ReportTable Data(params ReportCell[] cells)
            => Add(RowKind.Data, cells);

        /// <summary>A label in the first column and a value that spans the rest, left-aligned.</summary>
        public ReportTable Pair(string label, string value)
            => Add(RowKind.Data,
                   new ReportCell(label, Align.Left),
                   new ReportCell(value, Align.Left, ColumnCount() - 1));

        /// <summary>Total of a group. The label spans labelSpan columns; the values fill the columns after it.</summary>
        public ReportTable SubTotal(string label, int labelSpan, params string[] values)
            => AddSummary(RowKind.SubTotal, label, labelSpan, values);

        /// <summary>Grand total. The label spans labelSpan columns; the values fill the columns after it.</summary>
        public ReportTable Total(string label, int labelSpan, params string[] values)
            => AddSummary(RowKind.Total, label, labelSpan, values);

        // ═════════ 4. FINISH ════════════════════════════════════════════════════

        public SheetReport Build()
        {
            ColumnCount();   // makes sure columns exist

            Func<int, IHtmlContent>? columnHeader = null;
            if (_showColumnHeader)
                columnHeader = _ => RenderRow(new ReportRow(RowKind.Header,
                                        _columns.Select(c => new ReportCell(c.Title, c.Alignment)).ToArray()));

            return new SheetReport
            {
                Data = _model,
                Title = _title,
                SheetName = _sheetName,

                Cols = ColumnWidthsMm(),
                RowCount = _rows.Count,
                Subheader = RenderTitleBlock,
                ColHeader = columnHeader,
                Row = i => RenderRow(_rows[i]),
                KeepWithNext = KeepWithNext,

                RowMm = _rowMm,
                HeaderMm = _showColumnHeader ? _headerMm : 0,
                FirstPageExtraMm = _firstPageExtraMm,

                ExtraCss = _style.ToCss() + _extraCss
            };
        }

        // ═════════ INTERNALS ════════════════════════════════════════════════════

        private int ColumnCount() => _columns.Count > 0
            ? _columns.Count
            : throw new InvalidOperationException("Define the columns with .Column(...) before adding rows.");

        /// <summary>Adds a row after checking its cells cover exactly the table's columns.</summary>
        private ReportTable Add(RowKind kind, params ReportCell[] cells)
        {
            var columns = ColumnCount();
            var covered = cells.Sum(c => c.Span);
            if (covered != columns)
                throw new InvalidOperationException(
                    $"Row {_rows.Count + 1} ({kind}) covers {covered} column(s) but the table has {columns}. " +
                    "Pass one value per column, or use Pair / Section / Total for rows that span columns.");

            if (kind is RowKind.Section or RowKind.Group or RowKind.Header) _stripe = 0;
            var alt = kind == RowKind.Data && _stripe++ % 2 == 1;

            _rows.Add(new ReportRow(kind, cells, alt));
            return this;
        }

        /// <summary>Turns plain values into cells aligned like their column, starting at firstColumn.</summary>
        private ReportCell[] CellsFor(string[] values, int firstColumn = 0)
        {
            var cells = new ReportCell[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                var column = firstColumn + i;
                if (column >= ColumnCount())
                    throw new InvalidOperationException(
                        $"Row {_rows.Count + 1} has more values than the table has columns ({ColumnCount()}).");
                cells[i] = new ReportCell(values[i], _columns[column].Alignment);
            }
            return cells;
        }

        private ReportTable AddSummary(RowKind kind, string label, int labelSpan, string[] values)
        {
            var cells = new List<ReportCell> { new(label, Align.Right, labelSpan) };
            cells.AddRange(CellsFor(values, firstColumn: labelSpan));
            return Add(kind, cells.ToArray());
        }

        private double[] ColumnWidthsMm()
        {
            var printable = _page.ContentWidthMm - 1;
            var total = _columns.Sum(c => c.WidthPercent);
            return _columns.Select(c => Math.Round(printable * c.WidthPercent / total, 2)).ToArray();
        }

        /// <summary>A section, header or group never ends a page; a row just before a total moves with it.</summary>
        private bool KeepWithNext(int i) =>
            _rows[i].Kind is RowKind.Section or RowKind.Header or RowKind.Group
            || (i + 1 < _rows.Count && _rows[i + 1].Kind is RowKind.SubTotal or RowKind.Total);

        // ── rendering ──

        private IHtmlContent RenderTitleBlock(int columns)
        {
            var html = new HtmlContentBuilder();
            foreach (var line in _titleBlock) html.AppendHtml(line(columns));
            html.AppendHtml($"<tr class=\"meta-spacer\"><td colspan=\"{columns}\"></td></tr>");
            return html;
        }

        private IHtmlContent RenderRow(ReportRow row)
        {
            var css = $"{ClassOf(row.Kind)}{(row.Alt && _style.ZebraStripes ? " r-alt" : "")}";
            var html = new HtmlContentBuilder();
            html.AppendHtml($"<tr class=\"{css}\">");
            foreach (var cell in row.Cells) AppendCell(html, row.Kind, cell);
            html.AppendHtml("</tr>");
            return html;
        }

        private static void AppendCell(HtmlContentBuilder html, RowKind kind, ReportCell cell)
        {
            var tag = kind == RowKind.Header ? "th" : "td";
            var css = AlignClass(cell.Alignment) + (cell.Css is null ? "" : " " + cell.Css);
            var span = cell.Span > 1 ? $" colspan=\"{cell.Span}\"" : "";

            // long left-aligned text is clamped to the configured number of lines (keeps rows one height)
            var clamp = (kind is RowKind.Section or RowKind.Data or RowKind.Group or RowKind.SubTotal)
                        && cell.Alignment == Align.Left;

            html.AppendHtml($"<{tag} class=\"{E(css)}\"{span}>");
            if (clamp) html.AppendHtml("<div class=\"c\">");
            html.Append(cell.Text);                       // Append() HTML-encodes the text
            if (clamp) html.AppendHtml("</div>");
            html.AppendHtml($"</{tag}>");
        }

        // No default case on purpose: adding a RowKind without a css class here gives a compiler warning.
        private static string ClassOf(RowKind kind) => kind switch
        {
            RowKind.Section => "r-section",
            RowKind.Header => "r-header",
            RowKind.Data => "r-data",
            RowKind.Group => "r-group",
            RowKind.SubTotal => "r-subtotal",
            RowKind.Total => "r-total",
            RowKind.Spacer => "r-spacer",
            RowKind.Note => "r-note"
        };

        private static string AlignClass(Align a) => a switch
        {
            Align.Left => "text-left",
            Align.Center => "text-center",
            Align.Right => "text-right"
        };

        private static IHtmlContent Raw(string html) => new HtmlString(html);
        private static string E(string? text) => HtmlEncoder.Default.Encode(text ?? "");
    }
}