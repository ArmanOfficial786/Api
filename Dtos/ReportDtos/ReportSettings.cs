//using NexgenCosysReport.Utils.Enum;

//namespace NexgenCosysReport.Dtos.ReportDtos
//{
//    public class ReportSettings
//    {
//        public const string SectionName = "ReportSettings";

//        public string WebRootPath { get; set; } = string.Empty;
//    }
//    /// <summary>
//    /// Page size and margin settings for jsreport. Can be used in ReportRequestDtos to specify custom page sizes.
//    /// </summary>
//    public class PageSizeSetting
//    {
//        // ── Core properties ───────────────────────────────────────────
//        public PageFormat Format { get; set; } = PageFormat.A4;
//        public bool Landscape { get; set; } = false;
//        public double? CustomWidth { get; set; }
//        public double? CustomHeight { get; set; }
//        public PageUnit Unit { get; set; } = PageUnit.mm;
//        public string MarginTop { get; set; } = "10mm";
//        public string MarginBottom { get; set; } = "15mm";
//        public string MarginLeft { get; set; } = "10mm";
//        public string MarginRight { get; set; } = "10mm";

//        // ── Resolved properties (used by JsReportService) ─────────────

//        /// <summary>
//        /// Returns jsreport format string e.g. "A4", "Letter"
//        /// Returns null when Custom — Width/Height are used instead.
//        /// </summary>
//        public string? ResolvedFormat =>
//            Format == PageFormat.Custom ? null : Format.ToString();

//        /// <summary>
//        /// Returns width with unit e.g. "380mm", "11in"
//        /// Only populated when PageFormat is Custom.
//        /// </summary>
//        public string? ResolvedWidth =>
//            Format == PageFormat.Custom && CustomWidth.HasValue
//                ? $"{CustomWidth}{UnitString}"
//                : null;

//        /// <summary>
//        /// Returns height with unit e.g. "210mm"
//        /// Only populated when PageFormat is Custom.
//        /// </summary>
//        public string? ResolvedHeight =>
//            Format == PageFormat.Custom && CustomHeight.HasValue
//                ? $"{CustomHeight}{UnitString}"
//                : null;

//        private string UnitString => Unit switch
//        {
//            PageUnit.@in => "in",
//            _ => Unit.ToString()
//        };

//        // ── Named format presets ──────────────────────────────────────
//        public static PageSizeSetting A4Portrait => new() { Format = PageFormat.A4, Landscape = false };
//        public static PageSizeSetting A4Landscape => new() { Format = PageFormat.A4, Landscape = true };
//        public static PageSizeSetting A3Portrait => new() { Format = PageFormat.A3, Landscape = false };
//        public static PageSizeSetting A3Landscape => new() { Format = PageFormat.A3, Landscape = true };
//        public static PageSizeSetting A5Portrait => new() { Format = PageFormat.A5, Landscape = false };
//        public static PageSizeSetting A5Landscape => new() { Format = PageFormat.A5, Landscape = true };
//        public static PageSizeSetting LetterPortrait => new() { Format = PageFormat.Letter, Landscape = false };
//        public static PageSizeSetting LetterLandscape => new() { Format = PageFormat.Letter, Landscape = true };
//        public static PageSizeSetting LegalPortrait => new() { Format = PageFormat.Legal, Landscape = false };
//        public static PageSizeSetting LegalLandscape => new() { Format = PageFormat.Legal, Landscape = true };
//        public static PageSizeSetting TabloidPortrait => new() { Format = PageFormat.Tabloid, Landscape = false };
//        public static PageSizeSetting TabloidLandscape => new() { Format = PageFormat.Tabloid, Landscape = true };

//        // ── Custom size factories ─────────────────────────────────────

//        /// <summary>Custom page with default margins.</summary>
//        public static PageSizeSetting Custom(
//            double width,
//            double height,
//            PageUnit unit = PageUnit.mm,
//            bool landscape = false) => new()
//            {
//                Format = PageFormat.Custom,
//                CustomWidth = width,
//                CustomHeight = height,
//                Unit = unit,
//                Landscape = landscape
//            };

//        /// <summary>Custom page with explicit margins.</summary>
//        public static PageSizeSetting CustomWithMargins(
//            double width,
//            double height,
//            PageUnit unit,
//            bool landscape,
//            string marginTop = "10mm",
//            string marginBottom = "10mm",
//            string marginLeft = "10mm",
//            string marginRight = "10mm") => new()
//            {
//                Format = PageFormat.Custom,
//                CustomWidth = width,
//                CustomHeight = height,
//                Unit = unit,
//                Landscape = landscape,
//                MarginTop = marginTop,
//                MarginBottom = marginBottom,
//                MarginLeft = marginLeft,
//                MarginRight = marginRight
//            };
//    }

//}




using NexgenCosysReport.Utils.Enum;
using System.Globalization;

namespace NexgenCosysReport.Dtos.ReportDtos
{
    public class ReportSettings
    {
        public const string SectionName = "ReportSettings";

        public string WebRootPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Page size and margins. Serves jsreport (strings) AND the sheet views (numeric millimetres).
    /// Custom width/height accept mm, cm, in, px. Margins accept "10mm", "1cm", "0.5in", "12pt", "20px".
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
        public string MarginBottom { get; set; } = "15mm";
        public string MarginLeft { get; set; } = "10mm";
        public string MarginRight { get; set; } = "10mm";

        // ── Resolved properties for jsreport ──────────────────────────

        /// <summary>jsreport format ("A4", "Letter"); null when Custom.</summary>
        public string? ResolvedFormat =>
            Format == PageFormat.Custom ? null : Format.ToString();

        /// <summary>Width with unit ("380mm", "11in"); only for Custom.</summary>
        public string? ResolvedWidth =>
            Format == PageFormat.Custom && CustomWidth.HasValue
                ? $"{Num(CustomWidth.Value)}{UnitString}"
                : null;

        /// <summary>Height with unit ("210mm"); only for Custom.</summary>
        public string? ResolvedHeight =>
            Format == PageFormat.Custom && CustomHeight.HasValue
                ? $"{Num(CustomHeight.Value)}{UnitString}"
                : null;

        private string UnitString => Unit switch
        {
            PageUnit.@in => "in",
            _ => Unit.ToString()
        };

        // ── Numeric geometry in millimetres (used by sheet views) ─────

        /// <summary>Paper width in mm, orientation applied (landscape: width >= height).</summary>
        public double WidthMm => Math.Round(Oriented().W, 2);

        /// <summary>Paper height in mm, orientation applied.</summary>
        public double HeightMm => Math.Round(Oriented().H, 2);

        public double MarginTopMm => ToMm(MarginTop);
        public double MarginRightMm => ToMm(MarginRight);
        public double MarginBottomMm => ToMm(MarginBottom);
        public double MarginLeftMm => ToMm(MarginLeft);

        /// <summary>Sheet is 1mm shorter than the paper so it never spills onto a blank page.</summary>
        public double SheetHeightMm => HeightMm - 1.0;

        /// <summary>Printable width (inside left/right margins).</summary>
        public double ContentWidthMm => WidthMm - MarginLeftMm - MarginRightMm;

        /// <summary>Printable height (inside top/bottom margins).</summary>
        public double ContentHeightMm => SheetHeightMm - MarginTopMm - MarginBottomMm;

        /// <summary>Cache-key tag: size AND margins, so changing either never reuses stale HTML/PDF.</summary>
        public string CacheTag =>
            $"{Num(WidthMm)}x{Num(HeightMm)}_{Num(MarginTopMm)}-{Num(MarginRightMm)}-{Num(MarginBottomMm)}-{Num(MarginLeftMm)}";

        // Used only by the Loan Provision visual report (detail pages)
        public const double RowMm = 6.0;         // table row height (must match that view's CSS)
        public const double ReservedMm = 24.0;   // "Detail" title + table header + page footer

        public int DetailRowsPerPage =>
            Math.Max(1, (int)Math.Floor((SheetHeightMm - MarginTopMm - MarginBottomMm - ReservedMm) / RowMm) - 1);

        public int TotalPages(int rowCount) =>
            rowCount <= 0 ? 1 : 2 + (int)Math.Ceiling(rowCount / (double)DetailRowsPerPage);

        /// <summary>Whole rows that fit in availableMm.</summary>
        public static int RowsThatFit(double availableMm, double rowMm) =>
            rowMm <= 0 ? 0 : Math.Max(0, (int)Math.Floor(availableMm / rowMm));

        // ── Helpers ───────────────────────────────────────────────────

        private (double W, double H) Oriented()
        {
            double w, h;
            if (Format == PageFormat.Custom && CustomWidth.HasValue && CustomHeight.HasValue)
            {
                var f = UnitToMm(Unit);
                w = CustomWidth.Value * f;
                h = CustomHeight.Value * f;
            }
            else
            {
                (w, h) = Format switch
                {
                    PageFormat.A3 => (297.0, 420.0),
                    PageFormat.A5 => (148.0, 210.0),
                    PageFormat.Letter => (215.9, 279.4),
                    PageFormat.Legal => (215.9, 355.6),
                    PageFormat.Tabloid => (279.4, 431.8),
                    _ => (210.0, 297.0)          // A4 (and Custom without size)
                };
            }

            if (Landscape && h > w) (w, h) = (h, w);
            return (w, h);
        }

        private static double UnitToMm(PageUnit u) => u switch
        {
            PageUnit.cm => 10.0,
            PageUnit.@in => 25.4,
            PageUnit.px => 25.4 / 96,
            _ => 1.0
        };

        /// <summary>Parses "10mm", "1cm", "0.5in", "12pt", "20px" (no unit = mm).</summary>
        public static double ToMm(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            var s = value.Trim().ToLowerInvariant();
            double f = 1;
            if (s.EndsWith("mm")) s = s[..^2];
            else if (s.EndsWith("cm")) { s = s[..^2]; f = 10; }
            else if (s.EndsWith("in")) { s = s[..^2]; f = 25.4; }
            else if (s.EndsWith("pt")) { s = s[..^2]; f = 25.4 / 72; }
            else if (s.EndsWith("px")) { s = s[..^2]; f = 25.4 / 96; }
            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n * f : 0;
        }

        public static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        public static string Mm(double v) => Num(v) + "mm";

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

        /// <summary>Custom page with default margins (width/height in the given unit).</summary>
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

        /// <summary>Custom page with explicit margins (any unit string).</summary>
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
