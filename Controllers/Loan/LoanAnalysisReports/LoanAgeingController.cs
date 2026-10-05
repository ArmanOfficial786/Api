using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanAgeingController : ControllerBase
    {
        private readonly ILoanAgeingRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanAgeingController> _logger;

        private static readonly PageSizeSetting PageSetting =
           PageSizeSetting.Custom(594, 420, PageUnit.mm, landscape: true);

        public LoanAgeingController(
            ILoanAgeingRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanAgeingController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanAgeingRequestDto request,
            [FromQuery] string format = "VIEW",
             CancellationToken ct = default)
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



                var reportName = "LoanAgeing";
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
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? string.Empty);

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                var allRows = data.Sections.SelectMany(s => s.Rows).ToList();
                if (allRows.Count == 0)
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Sections", data.Sections ?? new List<LoanAgeingSectionDto>() },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "TotalOverDue", data.TotalOverDue },
                    { "TotalPrincipalAmount", data.TotalPrincipalAmount },
                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
                    { "TillDate", data.TillDateBs ?? string.Empty },
                    { "BranchName", data.BranchName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? string.Empty },
                    { "CollectorName", data.CollectorName ?? string.Empty },
                    { "PenaltyType", data.PenaltyType ?? "S" },
                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
                    { "AgeingOn", data.AgeingOn ?? "P" },
                    { "AgeingOnName", data.AgeingOnName ?? "Principle" },
                    { "ShowLoanIssueDate", data.ShowLoanIssueDate ?? "ID" },
                    { "ShowLoanIssueDateName", data.ShowLoanIssueDateName ?? "Issue Date" },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "IsNepaliReport", data.IsNepaliReport },
                    { "Format", upperFormat }
                };

                string viewPath;
                if (request.VisualReport)
                {
                    viewPath = "Views/VisualReport/Loan/VLoanAgeingReport.cshtml";
                }
                else if (request.IsNepaliReport)
                {
                    viewPath = "Views/Report/Loan/LoanAnalysisReports/LoanAgeingNepaliReport.cshtml";
                }
                else
                {
                    viewPath = "Views/Report/Loan/LoanAnalysisReports/LoanAgeingReport.cshtml";
                }

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: viewPath,
                        data: reportData));


                if (upperFormat == "VIEW")
                {
                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
                     htmlContent, reportKey, ct);

                    _logger.LogInformation("?? VIEW — jsreport Html recipe, {Bytes:N0} chars", viewHtml.Length);
                    return Content(viewHtml, "text/html");

                }

                if (upperFormat == "PDF")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        htmlContent, "PDF", reportKey, PageSetting, ct);
                    return _reportFileResponse.BuildPdfResponse(pdfBytes);
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, "MemberDetailReport",
                    _jsReportService, _logger, PageSetting, ct);
            }


            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating LoanAgeing report");
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