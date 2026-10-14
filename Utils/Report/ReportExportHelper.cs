







//using Microsoft.AspNetCore.Mvc;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Inteface.ReportInterface;

//namespace NexgenCosysReport.Utils.Report
//{
//    public static class ReportExportHelper
//    {
//        // ══════════════════════════════════════════════════════════════════
//        // Export from server cache — NO DB call
//        // ══════════════════════════════════════════════════════════════════

//        public static async Task<ActionResult> ExportFromCacheAsync(
//            string reportKey,
//            string format,
//            string reportName,
//            IJsReportService jsReportService,
//            ILogger logger,
//            PageSizeSetting? pageSetting = null,
//            CancellationToken ct = default)
//        {
//            // ✅ Uses TryGetCachedHtml — matches latest IJsReportService
//            if (!jsReportService.TryGetCachedHtml(reportKey, out var htmlContent)
//                || string.IsNullOrEmpty(htmlContent))
//            {
//                logger.LogWarning("⚠️ HTML not cached for key: {Key}", reportKey);
//                return new BadRequestObjectResult(new
//                {
//                    success = false,
//                    message = "Report not cached. Please view the report first."
//                });
//            }

//            // ✅ CancellationToken forwarded — matches latest interface signature
//            var fileBytes = await jsReportService.ExportReportToFormatAsync(
//                htmlContent,
//                format,
//                reportKey,
//                pageSetting,
//                ct);

//            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
//            var fileName = ReportUtils.GetFileName(reportName, format);

//            logger.LogInformation("✅ Export done: {Format} | {File}", format, fileName);

//            return new FileContentResult(fileBytes, contentType)
//            {
//                FileDownloadName = fileName
//            };
//        }

//        // ══════════════════════════════════════════════════════════════════
//        // Log cache state
//        // ══════════════════════════════════════════════════════════════════

//        public static void LogCacheState(
//            string format,
//            string reportKey,
//            bool TryGetCachedHtml,
//            ILogger logger)
//        {
//            logger.LogInformation("==========================================");
//            logger.LogInformation("FORMAT    : {Format}", format);
//            logger.LogInformation("CACHE KEY : {Key}", reportKey);
//            logger.LogInformation("IS CACHED : {TryGetCachedHtml}", TryGetCachedHtml);
//            logger.LogInformation("==========================================");
//        }

//        // In ReportExportHelper.cs
//        public static ActionResult ExportFromDiskAsync(
//            string pdfPath,
//            string format,
//            string reportName,
//            ILogger logger)
//        {
//            if (!System.IO.File.Exists(pdfPath))
//            {
//                logger.LogError("PDF file not found on disk: {Path}", pdfPath);
//                return new NotFoundObjectResult(new
//                {
//                    success = false,
//                    message = "Report file not found."
//                });
//            }

//            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
//            var fileName = ReportUtils.GetFileName(reportName, format);

//            logger.LogInformation("✅ Streaming PDF from disk: {File} ({MB:F2} MB)",
//                fileName, new FileInfo(pdfPath).Length / 1024.0 / 1024.0);

//            var fs = new FileStream(
//                pdfPath,
//                FileMode.Open,
//                FileAccess.Read,
//                FileShare.Read,
//                bufferSize: 81920,
//                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

//            return new FileStreamResult(fs, contentType)
//            {
//                FileDownloadName = fileName
//            };
//        }
//    }
//}






using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Services.ReportService;

namespace NexgenCosysReport.Utils.Report
{
    public static class ReportExportHelper
    {
        // Page + row count of a built VIEW. Kept here so a cache hit can still send full
        // pagination headers without touching the DB. Same lifetime as the HTML cache.
        private record ViewInfo(int Pages, int Records);
        private static readonly MemoryCache ViewInfoCache = new(new MemoryCacheOptions());
        private static readonly MemoryCacheEntryOptions ViewInfoOptions = new()
        {
            SlidingExpiration = TimeSpan.FromMinutes(30),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
        };

        // ══════════════════════════════════════════════════════════════════
        // STEP 1 — call BEFORE the DB.
        //   Cache hit  -> logs "CACHE HIT", serves from cache, returns the result.
        //   Cache miss -> logs "CACHE MISS", returns null (the controller then reads the DB).
        //   pdfView: true -> this report's VIEW is a PDF; false -> raw HTML.
        // ══════════════════════════════════════════════════════════════════
        public static async Task<ActionResult?> TryServeAsync(
            string reportKey, string format, string reportName, bool pdfView,
            IJsReportService js, IReportFileResponse response, ILogger logger,
            PageSizeSetting? page = null, CancellationToken ct = default)
        {
            var upper = format.ToUpperInvariant();

            if (!js.TryGetCachedHtml(reportKey, out var html) || string.IsNullOrEmpty(html))
            {
                logger.LogInformation("🗄️ CACHE MISS | {Format} | {Key} — reading from DB", upper, reportKey);
                return null;
            }

            logger.LogInformation("📦 CACHE HIT  | {Format} | {Key}", upper, reportKey);

            return upper != "VIEW"
                ? await ExportAsync(html, reportKey, upper, reportName, js, logger, page, ct)
                : await ViewAsync(html, reportKey, -1, pdfView, js, response, page, ct);   // -1: use the stored row count
        }

        // ══════════════════════════════════════════════════════════════════
        // STEP 2 — call AFTER the DB + Razor render (the HTML is already cached by
        //          RenderRazorToHtmlAndCacheAsync). Returns the final response.
        // ══════════════════════════════════════════════════════════════════
        public static Task<ActionResult> CompleteAsync(
            string html, string reportKey, string format, string reportName, bool pdfView, int totalRecords,
            IJsReportService js, IReportFileResponse response, ILogger logger,
            PageSizeSetting? page = null, CancellationToken ct = default)
        {
            var upper = format.ToUpperInvariant();

            return upper != "VIEW"
                ? ExportAsync(html, reportKey, upper, reportName, js, logger, page, ct)
                : ViewAsync(html, reportKey, totalRecords, pdfView, js, response, page, ct);
        }

        // ══════════════════════════════════════════════════════════════════
        // VIEW (PDF or raw HTML) + pagination headers
        // ══════════════════════════════════════════════════════════════════
        private static async Task<ActionResult> ViewAsync(
            string html, string reportKey, int records, bool pdfView,
            IJsReportService js, IReportFileResponse response,
            PageSizeSetting? page, CancellationToken ct)
        {
            var fresh = records >= 0;                                   // just built from the DB
            ViewInfoCache.TryGetValue(reportKey, out ViewInfo? info);
            if (!fresh) records = info?.Records ?? 0;                   // hit: restore the stored count

            ActionResult result;
            int pages;

            if (pdfView)
            {
                // returns the cached PDF; renders it from the HTML (and caches it) when missing
                var pdf = await js.ExportReportToFormatAsync(html, "PDF", reportKey, page, ct).ConfigureAwait(false);
                pages = (!fresh && info != null) ? info.Pages : JsReportService.CountPdfPages(pdf);
                result = new FileContentResult(pdf, "application/pdf");          // inline in the viewer
            }
            else
            {
                var view = await js.ExportReportToRawHtmlAsync(html, reportKey, page, ct).ConfigureAwait(false);
                pages = (!fresh && info != null) ? info.Pages : SheetHtml.CountSheets(view);
                result = new ContentResult { Content = view, ContentType = "text/html; charset=utf-8" };
            }

            ViewInfoCache.Set(reportKey, new ViewInfo(pages, records), ViewInfoOptions);
            response.AppendPaginationHeaders(pages, records);
            return result;
        }

        // ══════════════════════════════════════════════════════════════════
        // EXPORT (PDF / Excel / Word / ...) from the HTML
        // ══════════════════════════════════════════════════════════════════
        private static async Task<ActionResult> ExportAsync(
            string html, string reportKey, string format, string reportName,
            IJsReportService js, ILogger logger, PageSizeSetting? page, CancellationToken ct)
        {
            var fileBytes = await js.ExportReportToFormatAsync(html, format, reportKey, page, ct).ConfigureAwait(false);

            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
            var fileName = ReportUtils.GetFileName(reportName, format);

            logger.LogInformation("✅ Export done: {Format} | {File}", format, fileName);

            return new FileContentResult(fileBytes, contentType) { FileDownloadName = fileName };
        }

        // ══════════════════════════════════════════════════════════════════
        // Export from server cache — NO DB call (kept for controllers that still use it)
        // ══════════════════════════════════════════════════════════════════
        public static async Task<ActionResult> ExportFromCacheAsync(
           string reportKey,
            string format,
            string reportName,
            IJsReportService jsReportService,
            ILogger logger,
            PageSizeSetting? pageSetting = null,
            CancellationToken ct = default
            )
        {
            if (!jsReportService.TryGetCachedHtml(reportKey, out var htmlContent)
                || string.IsNullOrEmpty(htmlContent))
            {
                logger.LogWarning("⚠️ HTML not cached for key: {Key}", reportKey);
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Report not cached. Please view the report first."
                });
            }

            // ? Export from cache
            var fileBytes = await jsReportService.ExportReportToFormatAsync(
                htmlContent,
                format,
                reportKey,
                pageSetting);

            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
            var fileName = ReportUtils.GetFileName(reportName, format);

            logger.LogInformation("? Export done: {Format} | {File}", format, fileName);

            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // Log cache state (kept for controllers that still use it)
        // ══════════════════════════════════════════════════════════════════
        public static void LogCacheState(
            string format,
            string reportKey,
            bool TryGetCachedHtml,
            ILogger logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("FORMAT    : {Format}", format);
            logger.LogInformation("CACHE KEY : {Key}", reportKey);
            logger.LogInformation("IS CACHED : {TryGetCachedHtml}", TryGetCachedHtml);
            logger.LogInformation("==========================================");
        }

        // ══════════════════════════════════════════════════════════════════
        // Stream a PDF straight from disk
        // ══════════════════════════════════════════════════════════════════
        public static ActionResult ExportFromDiskAsync(
            string pdfPath,
            string format,
            string reportName,
            ILogger logger)
        {
            if (!System.IO.File.Exists(pdfPath))
            {
                logger.LogError("PDF file not found on disk: {Path}", pdfPath);
                return new NotFoundObjectResult(new
                {
                    success = false,
                    message = "Report file not found."
                });
            }

            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
            var fileName = ReportUtils.GetFileName(reportName, format);

            logger.LogInformation("✅ Streaming PDF from disk: {File} ({MB:F2} MB)",
                fileName, new FileInfo(pdfPath).Length / 1024.0 / 1024.0);

            var fs = new FileStream(
                pdfPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            return new FileStreamResult(fs, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }
}




