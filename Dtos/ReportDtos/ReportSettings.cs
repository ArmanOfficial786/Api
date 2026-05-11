using NexgenCosysReport.Utils.Enum;

namespace NexgenCosysReport.Dtos.ReportDtos
{
    public class ReportSettings
    {
        public const string SectionName = "ReportSettings";

        public string WebRootPath { get; set; } = string.Empty;
    }
    /// <summary>
    /// Page size and margin settings for jsreport. Can be used in ReportRequestDtos to specify custom page sizes.
    /// </summary>
    public class PageSizeSetting
    {
        // ── Core properties ───────────────────────────────────────────
        public PageFormat Format { get; set; } = PageFormat.A4;
        public bool Landscape { get; set; } = false;
        public double? CustomWidth { get; set; }
        public double? CustomHeight { get; set; }
        public PageUnit Unit { get; set; } = PageUnit.mm;
        public string MarginTop { get; set; } = "10mm";
        public string MarginBottom { get; set; } = "10mm";
        public string MarginLeft { get; set; } = "10mm";
        public string MarginRight { get; set; } = "10mm";

        // ── Resolved properties (used by JsReportService) ─────────────

        /// <summary>
        /// Returns jsreport format string e.g. "A4", "Letter"
        /// Returns null when Custom — Width/Height are used instead.
        /// </summary>
        public string? ResolvedFormat =>
            Format == PageFormat.Custom ? null : Format.ToString();

        /// <summary>
        /// Returns width with unit e.g. "380mm", "11in"
        /// Only populated when PageFormat is Custom.
        /// </summary>
        public string? ResolvedWidth =>
            Format == PageFormat.Custom && CustomWidth.HasValue
                ? $"{CustomWidth}{UnitString}"
                : null;

        /// <summary>
        /// Returns height with unit e.g. "210mm"
        /// Only populated when PageFormat is Custom.
        /// </summary>
        public string? ResolvedHeight =>
            Format == PageFormat.Custom && CustomHeight.HasValue
                ? $"{CustomHeight}{UnitString}"
                : null;

        private string UnitString => Unit switch
        {
            PageUnit.@in => "in",
            _ => Unit.ToString()
        };

        // ── Named format presets ──────────────────────────────────────
        public static PageSizeSetting A4Portrait => new() { Format = PageFormat.A4, Landscape = false };
        public static PageSizeSetting A4Landscape => new() { Format = PageFormat.A4, Landscape = true };
        public static PageSizeSetting A3Portrait => new() { Format = PageFormat.A3, Landscape = false };
        public static PageSizeSetting A3Landscape => new() { Format = PageFormat.A3, Landscape = true };
        public static PageSizeSetting A5Portrait => new() { Format = PageFormat.A5, Landscape = false };
        public static PageSizeSetting A5Landscape => new() { Format = PageFormat.A5, Landscape = true };
        public static PageSizeSetting LetterPortrait => new() { Format = PageFormat.Letter, Landscape = false };
        public static PageSizeSetting LetterLandscape => new() { Format = PageFormat.Letter, Landscape = true };
        public static PageSizeSetting LegalPortrait => new() { Format = PageFormat.Legal, Landscape = false };
        public static PageSizeSetting LegalLandscape => new() { Format = PageFormat.Legal, Landscape = true };
        public static PageSizeSetting TabloidPortrait => new() { Format = PageFormat.Tabloid, Landscape = false };
        public static PageSizeSetting TabloidLandscape => new() { Format = PageFormat.Tabloid, Landscape = true };

        // ── Custom size factories ─────────────────────────────────────

        /// <summary>Custom page with default margins.</summary>
        public static PageSizeSetting Custom(
            double width,
            double height,
            PageUnit unit = PageUnit.mm,
            bool landscape = false) => new()
            {
                Format = PageFormat.Custom,
                CustomWidth = width,
                CustomHeight = height,
                Unit = unit,
                Landscape = landscape
            };

        /// <summary>Custom page with explicit margins.</summary>
        public static PageSizeSetting CustomWithMargins(
            double width,
            double height,
            PageUnit unit,
            bool landscape,
            string marginTop = "10mm",
            string marginBottom = "10mm",
            string marginLeft = "10mm",
            string marginRight = "10mm") => new()
            {
                Format = PageFormat.Custom,
                CustomWidth = width,
                CustomHeight = height,
                Unit = unit,
                Landscape = landscape,
                MarginTop = marginTop,
                MarginBottom = marginBottom,
                MarginLeft = marginLeft,
                MarginRight = marginRight
            };
    }

}
