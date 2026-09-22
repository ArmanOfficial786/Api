// Controllers/Loan/LoanAnalysisReport/LoanAgingRiskCalculationController.cs
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
    public class LoanAgingRiskCalculationController : ControllerBase
    {
        private readonly ILoanAgingRiskCalculationRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanAgingRiskCalculationController> _logger;

        public LoanAgingRiskCalculationController(
            ILoanAgingRiskCalculationRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanAgingRiskCalculationController> logger)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanAgingRiskCalculationRequestDto request,
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



                var reportName = "LoanAgingRiskCalculation";
                var upperFormat = format.ToUpper();

                var reportKey = ReportUtils.GenerateReportKey(request, reportName);

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
                    { "TotalBalanceAmount", data.TotalBalanceAmount },
                    { "TotalOverDue", data.TotalOverDue },
                    { "TotalGoodLoan", data.TotalGoodLoan },
                    { "TotalArrear", data.TotalArrear },
                    { "TotalFrm1to365", data.TotalFrm1to365 },
                    { "TotalGrtthan365", data.TotalGrtthan365 },
                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "All" },
                    { "PenaltyType", data.PenaltyType ?? "S" },
                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Loan/VLoanAgingRiskCalculationReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReport/LoanAgingRiskCalculationReport.cshtml";

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
                _logger.LogError(ex, "Error generating LoanAgingRiskCalculation report");
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