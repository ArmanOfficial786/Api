using JsSampleReport.Inteface.ReportInterface;
using Microsoft.AspNetCore.Mvc;
namespace JsSampleReport.Utils.Report
{
    public static class ReportExportHelper
    {
        // ══════════════════════════════════════════════════════════════════
        // Export from server cache — NO DB call
        // Shared across all report controllers
        // ══════════════════════════════════════════════════════════════════
        public static IActionResult ExportFromCache(
            string reportKey,
            string format,
            string reportName,
            IJsReportService jsReportService,
            ILogger logger)
        {
            var cachedHtml = jsReportService.GetFromCache(reportKey);

            if (cachedHtml == null)
            {
                logger.LogWarning($"❌ Cache miss on export: {reportKey}");
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Report session expired. Please view the report again."
                });
            }

            logger.LogInformation($"✅ Exporting from cache: Key={reportKey}, Format={format}");

            var reportBytes = jsReportService.GenerateReportFromHtml(cachedHtml, format);
            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
            var fileName = ReportUtils.GetFileName(reportName, format);

            return new FileContentResult(reportBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // Log cache state — shared debug helper for all controllers
        // ══════════════════════════════════════════════════════════════════
        public static void LogCacheState(
            string format,
            string reportKey,
            bool isCached,
            ILogger logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation($"FORMAT    : {format}");
            logger.LogInformation($"CACHE KEY : {reportKey}");
            logger.LogInformation($"IS CACHED : {isCached}");
            logger.LogInformation("==========================================");
        }
    }
}
