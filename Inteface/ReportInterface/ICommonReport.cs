namespace JsSampleReport.Inteface.ReportInterface
{

    public interface IJsReportService
    {
        /// <summary>
        /// Generate report from HTML template
        /// </summary>
        /// <param name="reportPath">Path to HTML template file</param>
        /// <param name="data">Report data (can be object, dictionary, or list)</param>
        /// <param name="format">Output format (PDF, EXCEL, WORD, HTML, PNG)</param>
        /// <returns>Report as byte array</returns>
        byte[] GenerateReport(
            string reportPath,
            object data,
            string format);
        string RenderAndCacheReport(string reportKey, string reportPath, object data);
        byte[] GenerateReportFromHtml(string htmlContent, string format);
        bool IsCached(string reportKey);          // ✅ check if key exists
        string? GetFromCache(string reportKey);      // ✅ pull from cache
    }
}
