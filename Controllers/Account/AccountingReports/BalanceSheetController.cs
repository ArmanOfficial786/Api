using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.NexgenCosysReport.Dtos.RequestDtos.AccountOperation;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Account.AccountingReports
{
    [ApiController]
    [Route("api/[controller]")]
    public class BalanceSheetController : ControllerBase
    {
        private readonly IBalanceSheet _balanceSheetService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<BalanceSheetController> _logger;

        public BalanceSheetController(
            IBalanceSheet balanceSheetService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<BalanceSheetController> logger,
            CustomHeaderResponse headerResponse)
        {
            _balanceSheetService = balanceSheetService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }

        [HttpPost()]
        public async Task<ActionResult> GenerateReport(
            [FromBody] BalanceSheetRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "BalanceSheet";
                var upperFormat = format.ToUpper();


                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request" });



                var reportKey = ReportUtils.GenerateReportKey(request, reportName);

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);
                }

                // Fetch balance sheet data and header in parallel
                var dataTask = _balanceSheetService.GetBalanceSheetReport(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders(request.SameCompanyName ? "" : request.BranchIds);

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (data == null)
                    return NotFound(new { success = false, message = "No data found." });

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "BalanceSheetRows", data.Rows },
                    { "TotalDebit", data.TotalDebit },
                    { "TotalCredit", data.TotalCredit },
                    { "FiscalYearLabel", data.FiscalYearLabel },
                    { "TillDate", request.TillDate },
                    { "BranchName", request.BranchName },
                    { "ReportType", request.ReportType },
                    { "OrderBy", request.OrderBy },
                    { "IncludePreviousYearBalance", request.IncludePreviousYearBalance },
                    { "HeaderDataSet", headerData },
                    { "Format", upperFormat }
                };



                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/VisualReport/Account/VBalanceSheetReport.cshtml"
                            : "Views/Report/Account/BalanceSheetReport.cshtml",
                        data: reportData));
                if (upperFormat == "VIEW")
                {
                    if (request.VisualReport)
                    {
                        _headerResponse.SetResponseHeaders(true, 200, "Visual report generated successfully.");
                        Response.Headers.Append("X-Report-Format", "HTML");
                        Response.Headers.Append("X-Is-Visual-Report", "true");
                        Response.Headers.Append("X-Report-Name", reportName);
                        return Content(htmlContent, "text/html");
                    }

                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                                  htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false
                    };
                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

                    Response.Headers.Append(
                        "Content-Disposition",
                        "inline; filename=\"MemberIdCardReport.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }
                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "BalanceSheet report generation failed");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }
    }
}