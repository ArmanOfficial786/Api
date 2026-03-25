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
        //byte[] GenerateReport(
        //    string reportPath,
        //    object data,
        //    string format);
        //// ✅ Async — use in new controllers
        //Task<string> RenderAndCacheReport(string reportKey, string reportPath, object data);
        //Task<byte[]> GenerateReportFromHtml(string htmlContent, string format);
        //bool IsCached(string reportKey);          
        //string? GetFromCache(string reportKey);
        //

        // ✅ Cache helpers
        bool IsCached(string reportKey);
        string? GetFromCache(string reportKey);

        // ✅ Async only — all controllers use these
        Task<string> RenderAndCacheReportAsync(string reportKey, string reportPath, object data);
        Task<byte[]> GenerateReportFromHtmlAsync(string htmlContent, string format);
    }
}
