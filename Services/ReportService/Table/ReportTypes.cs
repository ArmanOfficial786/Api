using System.Net;

namespace NexgenCosysReport.Services.ReportService.Table
{
    /// <summary>Every row you add to a report has exactly one of these kinds.</summary>
    public enum RowKind
    {
        Section,    // full-width coloured bar            e.g. "Particulars"
        Header,     // column titles inside the table
        Data,       // an ordinary data row
        Group,      // full-width bold line that starts a group
        SubTotal,   // total of a group
        Total,      // grand total
        Spacer,     // small blank gap
        Note        // full-width small italic text
    }

    public enum Align { Left, Center, Right }

    /// <summary>One column: title, share of the printable width (%), default alignment.</summary>
    public sealed record ReportColumn(string Title, double WidthPercent, Align Alignment);

    /// <summary>One cell. Span = how many columns it covers.</summary>
    public sealed record ReportCell(string Text, Align Alignment = Align.Left, int Span = 1, string? Css = null, bool IsHtml = false)
    {
        public static ReportCell Left(string text, int span = 1) => new(text, Align.Left, span);
        public static ReportCell Center(string text, int span = 1) => new(text, Align.Center, span);
        public static ReportCell Right(string text, int span = 1) => new(text, Align.Right, span);

        /// <summary>Trusted HTML written as is (not encoded). Only pass markup you built yourself, never user input.</summary>
        public static ReportCell Raw(string html, Align alignment = Align.Left, int span = 1, string? css = null)
            => new(html, alignment, span, css, IsHtml: true);

        /// <summary>
        /// A picture from base64 data, e.g. a member photo. Null or empty data gives an empty cell.
        /// Accepts raw base64, or a full "data:image/...;base64,..." value. Scaled to fit by the "r-img" css.
        /// </summary>
        public static ReportCell Image(string? base64, string mimeType = "image/jpeg", Align alignment = Align.Center, int span = 1)
        {
            if (string.IsNullOrWhiteSpace(base64)) return new("", alignment, span);

            var data = base64.Trim();
            var src = data.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                ? WebUtility.HtmlEncode(data)
                : $"data:{WebUtility.HtmlEncode(mimeType)};base64,{WebUtility.HtmlEncode(data)}";

            return Raw($"<img class=\"r-img\" src=\"{src}\" alt=\"\" />", alignment, span);
        }
    }

    /// <summary>One printed row. Alt = shaded stripe (only drawn when the style asks for stripes).</summary>
    public sealed record ReportRow(RowKind Kind, IReadOnlyList<ReportCell> Cells, bool Alt = false);

    /// <summary>The look of a report. Change only what differs from the defaults.</summary>
    public sealed class ReportStyle
    {
        public string SectionBackground { get; init; } = "#e9ecef";
        public string SectionText { get; init; } = "#000";
        public string HeaderBackground { get; init; } = "#f9f9f9";
        public string HeaderText { get; init; } = "#000";
        public string DataText { get; init; } = "#000";
        public bool ZebraStripes { get; init; } = true;
        public string ZebraBackground { get; init; } = "#fafafa";
        public string GroupBackground { get; init; } = "#f2f2f2";
        public string SubTotalBackground { get; init; } = "#fcfcfc";
        public string TotalBackground { get; init; } = "#f9f9f9";
        public string LineColor { get; init; } = "#000";

        /// <summary>CSS for every row kind. Prefixed with table.sheet-table so it beats the engine defaults.</summary>
        public string ToCss() => $$"""
            .meta-label { white-space: nowrap; }
            table.sheet-table tr.r-section td  { background: {{SectionBackground}}; color: {{SectionText}}; font-weight: bold; border-top: 1px solid {{LineColor}}; border-bottom: 1px solid {{LineColor}}; }
            table.sheet-table tr.r-header th   { background: {{HeaderBackground}}; color: {{HeaderText}}; font-weight: bold; border-top: 2px solid {{LineColor}}; border-bottom: 1px solid {{LineColor}}; }
            table.sheet-table tr.r-data td     { color: {{DataText}}; border-bottom: 1px solid #eee; }
            table.sheet-table tr.r-data.r-alt td { background: {{ZebraBackground}}; }
            table.sheet-table tr.r-group td    { background: {{GroupBackground}}; font-weight: bold; border-top: 1px solid #999; border-bottom: 1px solid #999; }
            table.sheet-table tr.r-subtotal td { background: {{SubTotalBackground}}; font-weight: bold; border-top: 1px solid {{LineColor}}; }
            table.sheet-table tr.r-total td    { background: {{TotalBackground}}; font-weight: bold; border-top: 2px solid {{LineColor}}; border-bottom: 2px solid {{LineColor}}; }
            table.sheet-table tr.r-note td     { font-size: 8pt; font-style: italic; color: #444; }
            table.sheet-table tr.r-spacer      { height: 3mm; }
            table.sheet-table tr.r-spacer td   { padding: 0; border: 0; }
            table.sheet-table img.r-img        { display: block; margin: 0 auto; max-width: 100%; max-height: 13mm; object-fit: contain; }

            """;
    }
}