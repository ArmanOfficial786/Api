using NexgenCosysReport.Inteface.ReportInterface;
using Microsoft.AspNetCore.Mvc;

namespace NexgenCosysReport.Utils.Report
{
    public static class ReportExportHelper
    {
        // ------------------------------------------------------------------
        // Export from server cache — NO DB call
        // ------------------------------------------------------------------
        public static async Task<ActionResult> ExportFromCacheAsync(
            string reportKey,
            string format,
            string reportName,
            IJsReportService jsReportService,
            ILogger logger)
        {
            var htmlContent = jsReportService.GetCachedHtml(reportKey);
            if (string.IsNullOrEmpty(htmlContent))
            {
                logger.LogWarning("?? HTML not cached for key: {Key}", reportKey);
                return new BadRequestObjectResult(new
                {
                    success = false,
                    message = "Report not cached. Please view the report first."
                });
            }

            // ? Export from cache
            var fileBytes = await jsReportService.ExportReportToFormatAsync(
                htmlContent,
                format);

            var (contentType, _) = ReportUtils.GetContentTypeAndExtension(format);
            var fileName = ReportUtils.GetFileName(reportName, format);

            logger.LogInformation("? Export done: {Format} | {File}", format, fileName);

            return new FileContentResult(fileBytes, contentType)
            {
                FileDownloadName = fileName
            };
        }

        // ------------------------------------------------------------------
        // Log cache state
        // ------------------------------------------------------------------
        public static void LogCacheState(
            string format,
            string reportKey,
            bool IsHtmlCached,
            ILogger logger)
        {
            logger.LogInformation("==========================================");
            logger.LogInformation("FORMAT    : {Format}", format);
            logger.LogInformation("CACHE KEY : {Key}", reportKey);
            logger.LogInformation("IS CACHED : {IsHtmlCached}", IsHtmlCached);
            logger.LogInformation("==========================================");
        }
    }
}