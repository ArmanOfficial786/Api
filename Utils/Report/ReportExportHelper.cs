//using Microsoft.AspNetCore.Mvc;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Inteface.ReportInterface;

//namespace NexgenCosysReport.Utils.Report
//{
//    public static class ReportExportHelper
//    {
//        // ------------------------------------------------------------------
//        // Export from server cache — NO DB call
//        // ------------------------------------------------------------------
//        public static async Task<ActionResult> ExportFromCacheAsync(
//            string reportKey,
//            string format,
//            string reportName,
//            IJsReportService jsReportService,
//            ILogger logger,
//            PageSizeSetting? pageSetting = null
//            )
//        {
//            var htmlContent = jsReportService.GetCachedHtml(reportKey);
//            if (string.IsNullOrEmpty(htmlContent))
//            {
//                logger.LogWarning("?? HTML not cached for key: {Key}", reportKey);
//                return new BadRequestObjectResult(new
//                {
//                    success = false,
//                    message = "Report not cached. Please view the report first."
//                });
//            }

//            // ? Export from cache
//            var fileBytes = await jsReportService.ExportReportToFormatAsync(
//                htmlContent,
//                format,
//                reportKey,
//                pageSetting);

//            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
//            var fileName = ReportUtils.GetFileName(reportName, format);

//            logger.LogInformation("? Export done: {Format} | {File}", format, fileName);

//            return new FileContentResult(fileBytes, contentType)
//            {
//                FileDownloadName = fileName
//            };
//        }

//        // ------------------------------------------------------------------
//        // Log cache state
//        // ------------------------------------------------------------------
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
//    }
//}







using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;

namespace NexgenCosysReport.Utils.Report
{
    public static class ReportExportHelper
    {
        // ══════════════════════════════════════════════════════════════════
        // Export from server cache — NO DB call
        // ══════════════════════════════════════════════════════════════════

        public static async Task<ActionResult> ExportFromCacheAsync(
            string reportKey,
            string format,
            string reportName,
            IJsReportService jsReportService,
            ILogger logger,
            PageSizeSetting? pageSetting = null,
            CancellationToken ct = default)
        {
            // ✅ Uses TryGetCachedHtml — matches latest IJsReportService
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

            // ✅ CancellationToken forwarded — matches latest interface signature
            var fileBytes = await jsReportService.ExportReportToFormatAsync(
                htmlContent,
                format,
                reportKey,
                pageSetting,
                ct);

            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
            var fileName = ReportUtils.GetFileName(reportName, format);

            logger.LogInformation("✅ Export done: {Format} | {File}", format, fileName);

            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // Log cache state
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

        // In ReportExportHelper.cs
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