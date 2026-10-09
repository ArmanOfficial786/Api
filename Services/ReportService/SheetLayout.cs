//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Utils.Enum;
//using System.Globalization;
//using System.Text.RegularExpressions;

//namespace NexgenCosysReport.Services.ReportService
//{
//    public class SheetLayout
//    {
//        public const double RowMm = 6.0;        // table row height (must match the view's CSS)
//        public const double ReservedMm = 24.0;  // "Detail" title + table header + page footer

//        public double WidthMm { get; init; }
//        public double HeightMm { get; init; }
//        public double MarginTopMm { get; init; }
//        public double MarginRightMm { get; init; }
//        public double MarginBottomMm { get; init; }
//        public double MarginLeftMm { get; init; }

//        /// <summary>Sheet is 1mm shorter than the paper so it never spills onto a blank page.</summary>
//        public double SheetHeightMm => HeightMm - 1.0;

//        public int DetailRowsPerPage =>
//            Math.Max(1, (int)Math.Floor((SheetHeightMm - MarginTopMm - MarginBottomMm - ReservedMm) / RowMm) - 1); // -1 = room for Grand Total

//        /// <summary>Page 1 (KPIs + donuts) + page 2 (bar charts) + detail pages.</summary>
//        public int TotalPages(int rowCount) =>
//            rowCount <= 0 ? 1 : 2 + (int)Math.Ceiling(rowCount / (double)DetailRowsPerPage);

//        public string CacheTag => $"{Num(WidthMm)}x{Num(HeightMm)}";

//        // ── Build from your existing PageSizeSetting ──────────────────────
//        public static SheetLayout From(PageSizeSetting s)
//        {
//            double w, h;
//            if (s.Format == PageFormat.Custom && s.CustomWidth.HasValue && s.CustomHeight.HasValue)
//            {
//                var f = s.Unit.ToString().ToLowerInvariant() switch
//                {
//                    "cm" => 10.0,
//                    "in" => 25.4,
//                    "px" => 25.4 / 96,
//                    _ => 1.0
//                };
//                w = s.CustomWidth.Value * f;
//                h = s.CustomHeight.Value * f;
//            }
//            else
//            {
//                (w, h) = s.Format switch
//                {
//                    PageFormat.A3 => (297.0, 420.0),
//                    PageFormat.A5 => (148.0, 210.0),
//                    PageFormat.Letter => (215.9, 279.4),
//                    PageFormat.Legal => (215.9, 355.6),
//                    PageFormat.Tabloid => (279.4, 431.8),
//                    _ => (210.0, 297.0)
//                };
//            }

//            if (s.Landscape && h > w) (w, h) = (h, w);   // landscape guarantees width >= height

//            return new SheetLayout
//            {
//                WidthMm = Math.Round(w, 2),
//                HeightMm = Math.Round(h, 2),
//                MarginTopMm = ToMm(s.MarginTop),
//                MarginRightMm = ToMm(s.MarginRight),
//                MarginBottomMm = ToMm(s.MarginBottom),
//                MarginLeftMm = ToMm(s.MarginLeft)
//            };
//        }

//        // ── HTML markers (written by the view, read by the PDF exporter) ──
//        private static readonly Regex SizeRegex = new(
//            "data-fixed-sheets\\s+data-sheet-w=\"(?<w>[0-9.]+)\"\\s+data-sheet-h=\"(?<h>[0-9.]+)\"",
//            RegexOptions.Compiled);
//        //private static readonly Regex SheetRegex = new("class=\"report-page\"", RegexOptions.Compiled);
//        private static readonly Regex SheetRegex = new("class=\"(report-page|pg)\"", RegexOptions.Compiled);

//        public static bool TryReadSize(string html, out double w, out double h)
//        {
//            w = h = 0;
//            var m = SizeRegex.Match(html);
//            if (!m.Success) return false;
//            w = double.Parse(m.Groups["w"].Value, CultureInfo.InvariantCulture);
//            h = double.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
//            return true;
//        }

//        public static int CountSheets(string html) => SheetRegex.Matches(html).Count;

//        public static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
//        public static string Mm(double v) => Num(v) + "mm";

//        private static double ToMm(string? value)
//        {
//            if (string.IsNullOrWhiteSpace(value)) return 0;
//            var s = value.Trim().ToLowerInvariant();
//            double f = 1;
//            if (s.EndsWith("mm")) s = s[..^2];
//            else if (s.EndsWith("cm")) { s = s[..^2]; f = 10; }
//            else if (s.EndsWith("in")) { s = s[..^2]; f = 25.4; }
//            else if (s.EndsWith("pt")) { s = s[..^2]; f = 25.4 / 72; }
//            else if (s.EndsWith("px")) { s = s[..^2]; f = 25.4 / 96; }
//            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n * f : 0;
//        }
//    }
//}





//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Utils.Enum;
//using System.Globalization;
//using System.Text.RegularExpressions;

//namespace NexgenCosysReport.Services.ReportService
//{
//    /// <summary>
//    /// Page geometry in millimetres, built from PageSizeSetting.
//    /// Used by _SheetReport / SheetReport (views), the PDF exporter (size markers)
//    /// and the controllers (page count, cache key).
//    /// </summary>
//    public class SheetLayout
//    {
//        // Used only by the Loan Provision visual report (detail pages)
//        public const double RowMm = 6.0;        // table row height (must match the view's CSS)
//        public const double ReservedMm = 24.0;  // "Detail" title + table header + page footer

//        public double WidthMm { get; init; }
//        public double HeightMm { get; init; }
//        public double MarginTopMm { get; init; }
//        public double MarginRightMm { get; init; }
//        public double MarginBottomMm { get; init; }
//        public double MarginLeftMm { get; init; }

//        /// <summary>Sheet is 1mm shorter than the paper so it never spills onto a blank page.</summary>
//        public double SheetHeightMm => HeightMm - 1.0;

//        /// <summary>Printable width of one sheet (inside the left/right margins).</summary>
//        public double ContentWidthMm => WidthMm - MarginLeftMm - MarginRightMm;

//        /// <summary>Printable height of one sheet (inside the top/bottom margins).</summary>
//        public double ContentHeightMm => SheetHeightMm - MarginTopMm - MarginBottomMm;

//        /// <summary>Whole rows that fit in availableMm.</summary>
//        public static int RowsThatFit(double availableMm, double rowMm) =>
//            rowMm <= 0 ? 0 : Math.Max(0, (int)Math.Floor(availableMm / rowMm));

//        public int DetailRowsPerPage =>
//            Math.Max(1, (int)Math.Floor((SheetHeightMm - MarginTopMm - MarginBottomMm - ReservedMm) / RowMm) - 1); // -1 = room for Grand Total

//        /// <summary>Page 1 (KPIs + donuts) + page 2 (bar charts) + detail pages.</summary>
//        public int TotalPages(int rowCount) =>
//            rowCount <= 0 ? 1 : 2 + (int)Math.Ceiling(rowCount / (double)DetailRowsPerPage);

//        /// <summary>Cache-key tag: page size AND margins, so changing either never reuses stale HTML.</summary>
//        public string CacheTag =>
//            $"{Num(WidthMm)}x{Num(HeightMm)}_{Num(MarginTopMm)}-{Num(MarginRightMm)}-{Num(MarginBottomMm)}-{Num(MarginLeftMm)}";

//        // ── Build from PageSizeSetting ────────────────────────────────────
//        public static SheetLayout From(PageSizeSetting s)
//        {
//            double w, h;
//            if (s.Format == PageFormat.Custom && s.CustomWidth.HasValue && s.CustomHeight.HasValue)
//            {
//                var f = s.Unit.ToString().ToLowerInvariant() switch
//                {
//                    "cm" => 10.0,
//                    "in" => 25.4,
//                    "px" => 25.4 / 96,
//                    _ => 1.0
//                };
//                w = s.CustomWidth.Value * f;
//                h = s.CustomHeight.Value * f;
//            }
//            else
//            {
//                (w, h) = s.Format switch
//                {
//                    PageFormat.A3 => (297.0, 420.0),
//                    PageFormat.A5 => (148.0, 210.0),
//                    PageFormat.Letter => (215.9, 279.4),
//                    PageFormat.Legal => (215.9, 355.6),
//                    PageFormat.Tabloid => (279.4, 431.8),
//                    _ => (210.0, 297.0)          // A4
//                };
//            }

//            if (s.Landscape && h > w) (w, h) = (h, w);   // landscape guarantees width >= height

//            return new SheetLayout
//            {
//                WidthMm = Math.Round(w, 2),
//                HeightMm = Math.Round(h, 2),
//                MarginTopMm = ToMm(s.MarginTop),
//                MarginRightMm = ToMm(s.MarginRight),
//                MarginBottomMm = ToMm(s.MarginBottom),
//                MarginLeftMm = ToMm(s.MarginLeft)
//            };
//        }

//        // ── HTML markers (written by _SheetReport, read by the exporter) ──
//        private static readonly Regex WRegex = new("data-sheet-w=\"(?<v>[0-9.]+)\"", RegexOptions.Compiled);
//        private static readonly Regex HRegex = new("data-sheet-h=\"(?<v>[0-9.]+)\"", RegexOptions.Compiled);

//        // counts custom pages (class="report-page ...") and table-mode pages (<tbody class="pg">)
//        private static readonly Regex SheetRegex =
//            new("class=\"(report-page|pg)(\\s[^\"]*)?\"", RegexOptions.Compiled);

//        public static bool TryReadSize(string html, out double w, out double h)
//        {
//            w = h = 0;
//            if (string.IsNullOrEmpty(html) || !html.Contains("data-fixed-sheets", StringComparison.Ordinal))
//                return false;

//            var mw = WRegex.Match(html);
//            var mh = HRegex.Match(html);
//            if (!mw.Success || !mh.Success) return false;

//            w = double.Parse(mw.Groups["v"].Value, CultureInfo.InvariantCulture);
//            h = double.Parse(mh.Groups["v"].Value, CultureInfo.InvariantCulture);
//            return true;
//        }

//        /// <summary>Number of sheets in the HTML (= PDF page count). At least 1.</summary>
//        public static int CountSheets(string html) =>
//            string.IsNullOrEmpty(html) ? 1 : Math.Max(1, SheetRegex.Matches(html).Count);

//        // ── Formatting helpers used by the views ──────────────────────────
//        public static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
//        public static string Mm(double v) => Num(v) + "mm";

//        private static double ToMm(string? value)
//        {
//            if (string.IsNullOrWhiteSpace(value)) return 0;
//            var s = value.Trim().ToLowerInvariant();
//            double f = 1;
//            if (s.EndsWith("mm")) s = s[..^2];
//            else if (s.EndsWith("cm")) { s = s[..^2]; f = 10; }
//            else if (s.EndsWith("in")) { s = s[..^2]; f = 25.4; }
//            else if (s.EndsWith("pt")) { s = s[..^2]; f = 25.4 / 72; }
//            else if (s.EndsWith("px")) { s = s[..^2]; f = 25.4 / 96; }
//            return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n * f : 0;
//        }
//    }
//}