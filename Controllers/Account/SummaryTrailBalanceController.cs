using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Account
{
    [ApiController]
    [Route("api/[controller]")]
    public class SummaryTrialBalanceController : ControllerBase
    {
        private readonly ISummaryTrailBalance _summaryTrialBalance;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SummaryTrialBalanceController> _logger;

        public SummaryTrialBalanceController(
            ISummaryTrailBalance summaryTrialBalance,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<SummaryTrialBalanceController> logger,
            CustomHeaderResponse headerResponse)
        {
            _summaryTrialBalance = summaryTrialBalance;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] SummaryTrialBalanceRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "SummaryTrialBalance";
                var upperFormat = format.ToUpper();
                var response = new GeneralResponse<ReportResponseDtos>();

                if (request == null || string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate))
                {
                    response.isValid = false;
                    response.statusCode = 400;
                    response.message = "FromDate and ToDate are required.";
                    return BadRequest(response);
                }

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);
                }

                // Fetch data and header
                var dataTask = _summaryTrialBalance.GetSummaryTrialBalance(request);
                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Any())
                {
                    response.isValid = false;
                    response.statusCode = 404;
                    response.message = "No data found.";
                    return NotFound(response);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "TrialBalanceRows", data },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.BranchName },
                    { "OrderBy", request.OrderBy },
                    { "WithClosingBalance", request.WithClosingBalance },
                    { "ReportType", request.ReportType },
                    { "IsSubLedger", request.IsSubLedger },
                    { "HeaderDataSet", headerData },
                    { "Format", upperFormat }
                };

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/Report/Account/VSummaryTrailBalanceReport.cshtml"
                            : "Views/Report/Account/SummaryTrailBalanceReport.cshtml",
                        data: reportData));

                if (upperFormat == "VIEW")
                {

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
                        "inline; filename=\"SummaryTrialBalance.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SummaryTrialBalance report failed");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message, stack = ex.StackTrace });
            }
        }
    }
}