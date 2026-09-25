// Controllers/Loan/LoanAnalysisReport/CollectorWiseLoanAnalysisController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CollectorWiseLoanAnalysisController : ControllerBase
    {
        private readonly ICollectorWiseLoanAnalysisRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<CollectorWiseLoanAnalysisController> _logger;

        public CollectorWiseLoanAnalysisController(
            ICollectorWiseLoanAnalysisRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<CollectorWiseLoanAnalysisController> logger)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] CollectorWiseLoanAnalysisRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                }

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                if (string.IsNullOrWhiteSpace(request.FromDateBs) || request.FromDateBs == "-1")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select From Date" });
                }

                if (string.IsNullOrWhiteSpace(request.ToDateBs) || request.ToDateBs == "-1")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select To Date" });
                }

                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select Branch Name" });
                }

                if (request.PenaltyType != "S" && request.PenaltyType != "R" && request.PenaltyType != "A")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Penalty Type must be 'S' (Schedule Wise), 'R' (Remaining Principle), or 'A' (After Maturity)" });
                }

                if (request.ReportMode != "1" && request.ReportMode != "0")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Report Mode must be '1' (Summary) or '0' (Detail)" });
                }

                var reportName = "CollectorWiseLoanAnalysis";
                var upperFormat = format.ToUpper();

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }

                var dataTask = _repository.GetReportDataAsync(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "TotalPaidAmount", data.TotalPaidAmount },
                    { "TotalBalanceAmount", data.TotalBalanceAmount },
                    { "TotalGoodloan", data.TotalGoodloan },
                    { "TotalArrear", data.TotalArrear },
                    { "TotalArrearfrm1to365", data.TotalArrearfrm1to365 },
                    { "TotalArreargrtthan365", data.TotalArreargrtthan365 },
                    { "TotalOverDue", data.TotalOverDue },
                    { "TotalProvision", data.TotalProvision },
                    { "TotalOpeningBalance", data.TotalOpeningBalance },
                    { "TotalClosingBalance", data.TotalClosingBalance },
                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
                    { "FromDate", data.FromDateBs ?? "" },
                    { "ToDate", data.ToDateBs ?? "" },
                    { "FromDateAd", data.FromDateAd ?? "" },
                    { "ToDateAd", data.ToDateAd ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "CollectorName", data.CollectorName ?? "" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "PenaltyType", data.PenaltyType ?? "S" },
                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "EnableCollectionCenter", data.EnableCollectionCenter },
                    { "ReportMode", data.ReportMode ?? "1" },
                    { "ReportModeName", data.ReportModeName ?? "Summary" },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Loan/VCollectorWiseLoanAnalysisReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReport/CollectorWiseLoanAnalysisReport.cshtml";

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: viewPath,
                        data: reportData));

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false,
                        totalRecord = data.Rows.Count
                    };

                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    reportName,
                    _jsReportService, _logger);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CollectorWiseLoanAnalysis report");
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}