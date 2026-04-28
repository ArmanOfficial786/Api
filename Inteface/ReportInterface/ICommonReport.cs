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
        //bool IsHtmlCached(string reportKey);          
        //string? GetCachedHtml(string reportKey);
        //

        // ── HTML Cache ─────────────────────────────────────────────────────
        bool IsHtmlCached(string reportKey);
        string? GetCachedHtml(string reportKey);

        // ── PDF Cache ──────────────────────────────────────────────────────
        void StorePdfInCache(string reportKey, byte[] pdfBytes);
        byte[]? GetCachedPdf(string reportKey);

        // ✅ Async only — all controllers use these
        Task<string> RenderRazorToHtmlAndCacheAsync(string reportKey, string reportPath, object data);
        Task<byte[]> ExportReportToFormatAsync(string htmlContent, string format, string? reportKey = null);
    }
}
