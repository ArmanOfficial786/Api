namespace NexgenCosysReport.Inteface.ReportInterface
{

    public interface IJsReportService
    {
        /// <summary>
        /// Generate report from HTML template
        /// </summary>
        /// <param name="reportPath">Path to HTML template file</param>
        /// <param name="data">Report data (can be object, dictionary, or list)</param>
        /// <param name="format">Output format (PDF, EXCEL, HTML, PNG)</param>
        /// <returns>Report as byte array</returns>

        // -- HTML Cache -----------------------------------------------------
        bool IsHtmlCached(string reportKey);
        string? GetCachedHtml(string reportKey);

        // ? Async only — all controllers use these
        Task<string> RenderRazorToHtmlAndCacheAsync(string reportKey, string reportPath, object data);
        Task<byte[]> ExportReportToFormatAsync(string htmlContent, string format, string? reportKey = null);
    }
}
