
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
    public class LoanAllDetailsController : ControllerBase
    {
        private readonly ILoanAllDetailsRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanAllDetailsController> _logger;



        private static readonly PageSizeSetting PageSetting =
       PageSizeSetting.Custom(594, 420, PageUnit.mm, landscape: true);

        public LoanAllDetailsController(
            ILoanAllDetailsRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanAllDetailsController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanAllDetailsRequestDto request,
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



                var reportName = "LoanAllDetails";
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
                    { "TotalRemainingPrinciple", data.TotalRemainingPrinciple },
                    { "TotalDefaultPrinciple", data.TotalDefaultPrinciple },
                    { "TotalCurrInterest", data.TotalCurrInterest },
                    { "TotalCurrPenalty", data.TotalCurrPenalty },
                    { "TotalDue", data.TotalDue },
                    { "TotalRemaining", data.TotalRemaining },
                    { "TotalProvisionAmount", data.TotalProvisionAmount },
                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "CollectorName", data.CollectorName ?? "" },
                    { "LoanTypeName", data.LoanTypeName ?? "" },
                    { "StatusName", data.StatusName ?? "All" },
                    { "EnableCollectionCenter", request.EnableCollectionCenter },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Loan/VLoanAllDetailsReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanAllDetailsReport.cshtml";

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
                _logger.LogError(ex, "Error generating LoanAllDetails report");
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