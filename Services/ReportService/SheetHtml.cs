using System.Globalization;
using System.Text.RegularExpressions;

namespace NexgenCosysReport.Services.ReportService
{
    public static class SheetHtml
    {
        private static readonly Regex WRegex = new("data-sheet-w=\"(?<v>[0-9.]+)\"", RegexOptions.Compiled);
        private static readonly Regex HRegex = new("data-sheet-h=\"(?<v>[0-9.]+)\"", RegexOptions.Compiled);
        private static readonly Regex SheetRegex =
            new("class=\"(report-page|pg)(\\s[^\"]*)?\"", RegexOptions.Compiled);

        public static bool TryReadSize(string html, out double w, out double h)
        {
            w = h = 0;
            if (string.IsNullOrEmpty(html) || !html.Contains("data-fixed-sheets", StringComparison.Ordinal))
                return false;

            var mw = WRegex.Match(html);
            var mh = HRegex.Match(html);
            if (!mw.Success || !mh.Success) return false;

            w = double.Parse(mw.Groups["v"].Value, CultureInfo.InvariantCulture);
            h = double.Parse(mh.Groups["v"].Value, CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>Number of sheets in the HTML (= PDF page count). At least 1.</summary>
        public static int CountSheets(string html) =>
            string.IsNullOrEmpty(html) ? 1 : Math.Max(1, SheetRegex.Matches(html).Count);
    }
}
