//using Microsoft.AspNetCore.Html;
//using System.Net;
//using System.Text;

//namespace NexgenCosysReport.Services.ReportService
//{ /// <summary>One flattened line for grouped reports: H = group header, A = account/detail, T = total.</summary>
//    public record SheetLine(char Kind, int Group, int Item, int Sn);

//    /// <summary>Value == null means "serial number" column. Align: left | right | center.</summary>
//    public record SheetColumn<T>(string Header, Func<T, string?>? Value = null, double WidthMm = 0, string Align = "left");

//    public interface ISheetReport
//    {
//        Dictionary<string, object> Data { get; }
//        PageSizeSetting  Layout { get; }
//        string Title { get; }
//        string SheetName { get; }
//        bool RepeatHeader { get; }
//        bool Bordered { get; }
//        double RowMm { get; }
//        double HeaderMm { get; }
//        int CellLines { get; }
//        string? ExtraCss { get; }
//        string EmptyText { get; }
//        bool HasRows { get; }
//        double FooterMm { get; }
//        IReadOnlyList<double> Cols { get; }
//        IReadOnlyList<IReadOnlyList<object>> PageLines { get; }
//        IHtmlContent RenderSubheader();
//        IHtmlContent RenderColHeader();
//        IHtmlContent RenderRow(object line, int index);
//        IHtmlContent RenderAfterTable();
//        IHtmlContent RenderExcel();
//    }

//    public class SheetReport<T> : ISheetReport
//    {
//        // ── required ───────────────────────────────────────────────────
//        public required Dictionary<string, object> Data { get; init; }
//        public required IReadOnlyList<T> Lines { get; init; }
//        public required string Title { get; init; }
//        public required string SheetName { get; init; }

//        // ── simple reports: declare columns, everything else is generated ──
//        public IReadOnlyList<SheetColumn<T>>? Columns { get; init; }
//        public IReadOnlyList<(string Left, string Right)> Meta { get; init; } = [];
//        public string? TotalText { get; init; }

//        // ── custom reports: Razor templates override the defaults ──────
//        public IReadOnlyList<double>? ColWidths { get; init; }
//        public Func<object?, IHtmlContent>? Subheader { get; init; }
//        public Func<object?, IHtmlContent>? ColHeader { get; init; }
//        public Func<T, IHtmlContent>? Row { get; init; }
//        public Func<object?, IHtmlContent>? AfterTable { get; init; }
//        public Func<object?, IHtmlContent>? Excel { get; init; }
//        public Func<T, bool>? KeepWithNext { get; init; }   // e.g. group header must not end a page

//        // ── tuning (mm) ────────────────────────────────────────────────
//        public double RowMm { get; init; } = 7;
//        public double HeaderMm { get; init; } = 8;
//        public double FooterMm { get; init; } = 9;
//        public double SafetyMm { get; init; } = 2;
//        public double FirstPageExtraMm { get; init; } = 70;  // common header + title + meta on page 1
//        public int AfterTableRows { get; init; } = 0;        // rows of space the AfterTable block needs
//        public int CellLines { get; init; } = 1;
//        public bool RepeatHeader { get; init; } = false;
//        public bool Bordered { get; init; }
//        public bool Zebra { get; init; }
//        public string? ExtraCss { get; init; }
//        public string EmptyText { get; init; } = "No data found";

//        // ── derived ────────────────────────────────────────────────────
//        public PageSizeSetting  Layout =>
//            Data.TryGetValue("PageSizeSetting ", out var l) && l is PageSizeSetting  v
//                ? v
//                : throw new InvalidOperationException("PageSizeSetting  missing. Add { \"PageSizeSetting \", Layout } in the controller.");

//        public bool HasRows => Lines.Count > 0;

//        public IReadOnlyList<double> Cols =>
//            ColWidths ?? Columns?.Select(c => c.WidthMm).ToList() ?? new List<double>();

//        private List<List<T>>? _pages;
//        private List<List<T>> Pages => _pages ??= Paginate();

//        public IReadOnlyList<IReadOnlyList<object>> PageLines =>
//            Pages.Select(p => (IReadOnlyList<object>)p.Cast<object>().ToList()).ToList();

//        private List<List<T>> Paginate()
//        {
//            var l = Layout;
//            var body = l.SheetHeightMm - l.MarginTopMm - l.MarginBottomMm - FooterMm - SafetyMm;
//            var first = Math.Max(1, (int)Math.Floor((body - FirstPageExtraMm - HeaderMm) / RowMm));
//            var next = Math.Max(2, (int)Math.Floor((body - (RepeatHeader ? HeaderMm : 0)) / RowMm));

//            var pages = new List<List<T>> { new() };
//            var cap = first;
//            foreach (var item in Lines)
//            {
//                if (pages[^1].Count >= cap)
//                {
//                    var prev = pages[^1];
//                    var carry = new List<T>();
//                    if (KeepWithNext != null && prev.Count > 1 && KeepWithNext(prev[^1]))
//                    {
//                        carry.Add(prev[^1]);
//                        prev.RemoveAt(prev.Count - 1);
//                    }
//                    pages.Add(carry);
//                    cap = next;
//                }
//                pages[^1].Add(item);
//            }

//            // make room for the block under the table on the last page
//            if (AfterTableRows > 0)
//            {
//                var last = pages[^1];
//                var lastCap = pages.Count == 1 ? first : next;
//                var move = Math.Min(last.Count - 1, last.Count - (lastCap - AfterTableRows));
//                if (move > 0)
//                {
//                    var moved = last.GetRange(last.Count - move, move);
//                    last.RemoveRange(last.Count - move, move);
//                    pages.Add(moved);
//                }
//            }
//            return pages;
//        }

//        // ── rendering (custom template if given, else default) ─────────
//        public IHtmlContent RenderSubheader() => Subheader?.Invoke(null) ?? DefaultSubheader();
//        public IHtmlContent RenderColHeader() => ColHeader?.Invoke(null) ?? DefaultColHeader();
//        public IHtmlContent RenderRow(object line, int index) =>
//            Row != null ? Row((T)line) : DefaultRow((T)line, index);
//        public IHtmlContent RenderAfterTable() => AfterTable?.Invoke(null) ?? DefaultAfterTable();
//        public IHtmlContent RenderExcel() => Excel?.Invoke(null) ?? DefaultExcel();

//        private static string E(string? s) => WebUtility.HtmlEncode(s ?? "");
//        private static string Cls(string align) =>
//            align == "right" ? " class=\"text-right\"" : align == "center" ? " class=\"text-center\"" : "";

//        private IHtmlContent DefaultSubheader()
//        {
//            var sb = new StringBuilder($"<div class=\"subheader-title\">{E(Title)}</div>");
//            foreach (var (left, right) in Meta)
//                sb.Append($"<div class=\"subheader-meta two\"><span>{E(left)}</span><span>{E(right)}</span></div>");
//            sb.Append("<div class=\"subheader-spacer\"></div>");
//            return new HtmlString(sb.ToString());
//        }

//        private IHtmlContent DefaultColHeader()
//        {
//            var sb = new StringBuilder("<tr>");
//            foreach (var c in Columns!)
//                sb.Append($"<th{Cls(c.Align)}><div class=\"c\">{E(c.Header)}</div></th>");
//            return new HtmlString(sb.Append("</tr>").ToString());
//        }

//        private IHtmlContent DefaultRow(T item, int index)
//        {
//            var sb = new StringBuilder(Zebra && index % 2 == 1 ? "<tr class=\"alt\">" : "<tr>");
//            foreach (var c in Columns!)
//            {
//                var v = c.Value == null ? (index + 1).ToString() : c.Value(item);
//                sb.Append($"<td{Cls(c.Align)}><div class=\"c\">{E(v)}</div></td>");
//            }
//            return new HtmlString(sb.Append("</tr>").ToString());
//        }

//        private IHtmlContent DefaultAfterTable() =>
//            string.IsNullOrEmpty(TotalText)
//                ? HtmlString.Empty
//                : new HtmlString($"<div class=\"grand-total-box\">{E(TotalText)}</div>");

//        private IHtmlContent DefaultExcel()
//        {
//            var n = Math.Max(1, Columns!.Count);
//            var left = n / 2;
//            var sb = new StringBuilder("<table class=\"data-table\"><tbody>");
//            sb.Append($"<tr class=\"meta-title\"><td colspan=\"{n}\">{E(Title)}</td></tr>");
//            foreach (var (l, r) in Meta)
//                sb.Append($"<tr class=\"meta-row\"><td colspan=\"{left}\">{E(l)}</td><td colspan=\"{n - left}\" class=\"text-right\">{E(r)}</td></tr>");
//            sb.Append($"<tr class=\"meta-spacer\"><td colspan=\"{n}\"></td></tr><tr>");
//            foreach (var c in Columns) sb.Append($"<th{Cls(c.Align)}>{E(c.Header)}</th>");
//            sb.Append("</tr>");
//            var i = 0;
//            foreach (var item in Lines)
//            {
//                sb.Append("<tr class=\"detail-row\">");
//                foreach (var c in Columns)
//                    sb.Append($"<td{Cls(c.Align)}>{E(c.Value == null ? (i + 1).ToString() : c.Value(item))}</td>");
//                sb.Append("</tr>");
//                i++;
//            }
//            if (!string.IsNullOrEmpty(TotalText))
//                sb.Append($"<tr class=\"grand-total\"><td colspan=\"{n}\">{E(TotalText)}</td></tr>");
//            return new HtmlString(sb.Append("</tbody></table>").ToString());
//        }
//    }
//}

using Microsoft.AspNetCore.Html;
using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Services.ReportService
{
    public sealed class SheetReport
    {
        public const double FooterMm = 4;   // height of the "Page X of N" row
        public const double SafetyMm = 2;

        // ── required ──────────────────────────────────────────────
        public required Dictionary<string, object> Data { get; init; }   // needs "VisualLayout" or "PageSizeSetting "
        public required string Title { get; init; }
        public required string SheetName { get; init; }

        // ── tabular mode: ONE table for html / pdf / excel ────────
        public double[] Cols { get; init; } = [];                        // mm, 0 = flexible
        public int RowCount { get; init; }
        public Func<int, IHtmlContent>? Row { get; init; }               // <tr> for row i
        public Func<int, IHtmlContent>? Subheader { get; init; }         // <tr> rows, arg = column count
        public Func<int, IHtmlContent>? ColHeader { get; init; }         // one <tr> of <th>
        public Func<int, bool>? KeepWithNext { get; init; }              // row i must not end a page

        // ── custom mode (visual reports with charts) ──────────────
        public Func<object?, IHtmlContent>? Body { get; init; }          // .report-page divs (+ your script)
        public Func<object?, IHtmlContent>? ExcelBody { get; init; }     // single Excel <table> (hidden in html/pdf)
        public Func<object?, IHtmlContent>? Head { get; init; }          // extra <head> content
        public bool DeferReady { get; init; }                            // true: your script calls window.reportReady()

        // ── tuning ────────────────────────────────────────────────
        public double RowMm { get; init; } = 7;
        public double HeaderMm { get; init; } = 8;
        public double FirstPageExtraMm { get; init; } = 70;              // common header + title + meta on page 1
        public int CellLines { get; init; } = 1;
        public bool Bordered { get; init; }
        public string? ExtraCss { get; init; }
        public string EmptyText { get; init; } = "No data found for the selected criteria";

        // ── derived ───────────────────────────────────────────────
        public PageSizeSetting Layout =>
        Data.TryGetValue("PageSetting", out var p) && p is PageSizeSetting ps
            ? ps
            : throw new InvalidOperationException("PageSetting missing in the report data.");

        public int ColCount => Cols.Length;

        private List<(int Start, int Count)>? _pages;
        public List<(int Start, int Count)> Pages =>
            _pages ??= Plan(Layout, RowCount, RowMm, ColHeader == null ? 0 : HeaderMm,
                            FirstPageExtraMm, false, KeepWithNext);

        /// <summary>Blank height (mm) that pushes the "Page X of N" row to the bottom of page p.</summary>
        public double Filler(int p, int count)
        {
            var l = Layout;
            var body = l.SheetHeightMm - l.MarginTopMm - l.MarginBottomMm - FooterMm - SafetyMm;
            var used = (p == 0 ? FirstPageExtraMm + (ColHeader == null ? 0 : HeaderMm) : 0) + count * RowMm;
            return Math.Max(0, body - used);
        }

        /// <summary>Splits rowCount rows into pages. Also used by visual reports for their detail pages.</summary>
        public static List<(int Start, int Count)> Plan(
            PageSizeSetting l, int rowCount, double rowMm,
            double headerMm = 0, double firstExtraMm = 0,
            bool repeatHeader = false, Func<int, bool>? keepWithNext = null)
        {
            var body = l.SheetHeightMm - l.MarginTopMm - l.MarginBottomMm - FooterMm - SafetyMm;
            var first = Math.Max(1, (int)Math.Floor((body - firstExtraMm - headerMm) / rowMm));
            var next = Math.Max(2, (int)Math.Floor((body - (repeatHeader ? headerMm : 0)) / rowMm));

            var pages = new List<(int, int)>();
            var idx = 0;
            do
            {
                var cap = pages.Count == 0 ? first : next;
                var take = Math.Min(cap, rowCount - idx);
                if (idx + take < rowCount && take > 1 && keepWithNext?.Invoke(idx + take - 1) == true)
                    take--;                                  // a group header never ends a page
                pages.Add((idx, take));
                idx += take;
            } while (idx < rowCount);
            return pages;
        }
    }
}