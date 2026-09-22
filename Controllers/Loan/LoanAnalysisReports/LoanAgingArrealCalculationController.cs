
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
using System.Text;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanAgingArrealCalculationController : ControllerBase
    {
        private readonly ILoanAgingArrealCalculationRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanAgingArrealCalculationController> _logger;

        public LoanAgingArrealCalculationController(
            ILoanAgingArrealCalculationRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanAgingArrealCalculationController> logger)
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
            [FromBody] LoanAgingArrealCalculationRequestDto request,
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



                var reportName = "LoanAgingArrealCalculation";
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
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "CollectorName", data.CollectorName ?? "" },
                    { "Enable1To30Days", data.Enable1To30Days },
                    { "EnableCollectionCenter", request.EnableCollectionCenter },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Loan/VLoanAgingArrealCalculationReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReport/LoanAgingArrealCalculationReport.cshtml";

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
                _logger.LogError(ex, "Error generating LoanAgingArrealCalculation report");
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }

        [HttpPost("export-excel")]
        public async Task<IActionResult> ExportExcel(
            [FromBody] LoanAgingArrealCalculationRequestDto request)
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

                if (string.IsNullOrWhiteSpace(request.TillDateBs) || request.TillDateBs == "-1")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select Till Date" });
                }

                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select Branch Name" });
                }

                var rows = await _repository.GetExcelExportDataAsync(request);

                if (!rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var sb = new StringBuilder();

                sb.AppendLine("Member Id\tLoan Account No\tFull Name\tLoan Type\tLoan Issue Date (BS)\tLoan Issue Amount\tPayment Mode\tMaturity Date (BS)\tLast Installment Date\tDefaulter Days\tPrinciple Paid Amount\tLoan Balance\tGood Loan\tBetween 0-30 Days\tBetween 31-365 Days\tGreater Than 365 Days\tOverdue");

                foreach (var row in rows)
                {
                    sb.AppendLine(string.Join("\t",
                        row.MemberId ?? "",
                        row.LoanAccountNo ?? "",
                        row.FullName ?? "",
                        row.LoanTypeName ?? "",
                        row.LoanIssueOnBs ?? "",
                        row.LoanIssueAmount?.ToString("N2") ?? "",
                        row.PaymentMode ?? "",
                        row.MaturityOnBs ?? "",
                        row.LastInstallmentDate ?? "",
                        row.DefaulterDay?.ToString() ?? "0",
                        row.PrinciplePaidAmt?.ToString("N2") ?? "",
                        row.LoanBalance?.ToString("N2") ?? "",
                        row.Goodloan?.ToString("N2") ?? "",
                        row.Between0to30Days?.ToString("N2") ?? "",
                        row.Between31to365Days?.ToString("N2") ?? "",
                        row.GraterThan365Days?.ToString("N2") ?? "",
                        row.Overdue?.ToString("N2") ?? ""
                    ));
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());

                _headerResponse.SetResponseHeaders(true, 200, "Excel exported successfully.");
                Response.Headers.Append("Content-Disposition", "attachment; filename=LoanAgeing.xls");

                return new FileContentResult(bytes, "application/vnd.ms-excel");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting LoanAgingArrealCalculation report to Excel");
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