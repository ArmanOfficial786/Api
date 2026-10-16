using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanCICController : ControllerBase
    {
        private const string ReportName = "LoanCIC";

        private readonly ILoanCICRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LoanCICController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: wide page (20 columns); visual: same page as the other visual reports
        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(594, 420, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LoanCICController(
            ILoanCICRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LoanCICController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanCICRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;
                var viewType = isVisual ? "VISUAL" : "NORMAL";

                // the DB data is cached once and shared by the normal and the visual view
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{viewType}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync(request.BranchIds, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null || loaded.Rows == null || !loaded.Rows.Any())
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "TotalPriPaid", data.TotalPriPaid },
                    { "TotalPriBalance", data.TotalPriBalance },
                    { "TotalIntBalance", data.TotalIntBalance },
                    { "TotalPenaltyBalance", data.TotalPenaltyBalance },
                    { "TotalBalance", data.TotalBalance },
                    { "TotalGoodLoan", data.TotalGoodLoan },
                    { "TotalLoanRisk30", data.TotalLoanRisk30 },
                    { "TotalLoanRisk31to365", data.TotalLoanRisk31to365 },
                    { "TotalLoanRisk1to365", data.TotalLoanRisk1to365 },
                    { "TotalLoanRiskGreaterThan365", data.TotalLoanRiskGreaterThan365 },
                    { "HeaderDataSet", await headerTask },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "TillDateAd", data.TillDateAd ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "LoanTypeName", data.LoanTypeName ?? "" },
                    { "ProvisionType", data.ProvisionType ?? "S" },
                    { "ProvisionTypeName", data.ProvisionTypeName ?? "Schedule Wise" },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "Enable1To30Days", data.Enable1To30Days },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanCICReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanCICReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Rows.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}