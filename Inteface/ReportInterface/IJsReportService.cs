//using NexgenCosysReport.Dtos.ReportDtos;

//namespace NexgenCosysReport.Inteface.ReportInterface
//{

//    public interface IJsReportService
//    {
//        /// <summary>
//        /// Generate report from HTML template
//        /// </summary>
//        /// <param name="reportPath">Path to HTML template file</param>
//        /// <param name="data">Report data (can be object, dictionary, or list)</param>
//        /// <param name="format">Output format (PDF, EXCEL, HTML, PNG)</param>
//        /// <returns>Report as byte array</returns>

//        // -- HTML Cache -----------------------------------------------------
//        bool TryGetCachedHtml(string reportKey);
//        string? GetCachedHtml(string reportKey);

//        // ? Async only — all controllers use these
//        Task<string> RenderRazorToHtmlAndCacheAsync(string reportKey, string reportPath, object data);
//        Task<byte[]> ExportReportToFormatAsync(string htmlContent, string format, string? reportKey = null, PageSizeSetting? pageSetting = null);

//        // ── Chunked PDF export for large datasets ──────────────────────────
//        // Same style as ExportReportToFormatAsync:
//        // Pass reportData dict + rowsKey (which key holds the rows list) + chunkSize
//        // Service splits rows, renders each chunk, merges all PDFs internally
//        Task<byte[]> ExportChunkedPdfAsync(
//            string reportKey,
//            string reportPath,
//            Dictionary<string, object> reportData,
//            string rowsKey,
//            int chunkSize,
//            PageSizeSetting? pageSetting = null);
//    }
//}





//using NexgenCosysReport.Dtos.ReportDtos;

//namespace NexgenCosysReport.Inteface.ReportInterface
//{
//    public interface IJsReportService
//    {
//        // ── Cache ──────────────────────────────────────────────────────────
//        bool TryGetCachedHtml(string reportKey, out string? html);
//        bool TryGetCachedPdf(string reportKey, out byte[]? pdf);
//        /// <summary>
//        /// If a chunked PDF was previously generated for this reportKey, returns its file path.
//        /// </summary>
//        bool TryGetChunkedPdfPath(string reportKey, out string? pdfPath);

//        // ── Render Razor → HTML → Cache ────────────────────────────────────
//        Task<string> RenderRazorToHtmlAndCacheAsync(
//            string reportKey,
//            string reportPath,
//            object data,
//            CancellationToken ct = default);

//        // ── Export cached HTML → any format ────────────────────────────────
//        Task<byte[]> ExportReportToFormatAsync(
//            string htmlContent,
//            string format,
//            string? reportKey = null,
//            PageSizeSetting? pageSetting = null,
//            CancellationToken ct = default);

//        // ── Chunked PDF export for large datasets ──────────────────────────
//        Task<string> ExportChunkedPdfAsync(
//            string reportKey,
//            string reportPath,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            int chunkSize,
//            PageSizeSetting? pageSetting = null,
//            int maxParallelism = 2,
//            CancellationToken ct = default);
//    }
//}






using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IJsReportService
    {
        // ── Cache ──────────────────────────────────────────────────────────
        bool TryGetCachedHtml(string reportKey, out string? html);
        bool TryGetCachedPdf(string reportKey, out byte[]? pdf);

        // ── Render Razor → HTML → Cache ────────────────────────────────────
        Task<string> RenderRazorToHtmlAndCacheAsync(
            string reportKey,
            string reportPath,
            object data,
            CancellationToken ct = default);

        // ── Export cached HTML → any format ────────────────────────────────
        Task<byte[]> ExportReportToFormatAsync(
            string htmlContent,
            string format,
            string? reportKey = null,
            PageSizeSetting? pageSetting = null,
            CancellationToken ct = default);
        Task<string> ExportReportToRawHtmlAsync(
            string htmlContent,
            string? reportKey = null,
            CancellationToken ct = default);


    }
}