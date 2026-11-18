using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
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
    public class LoanAllDetailsController : ControllerBase
    {
        private const string ReportName = "LoanAllDetails";

        private readonly ILoanAllDetailsRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanAllDetailsController> _logger;

        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(700, 420, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LoanAllDetailsController(
            ILoanAllDetailsRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanAllDetailsController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
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
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                // default request = no column list -> all columns; specific = only those; visual = always ALL
                var selectedColumns = isVisual
                    ? new List<string>()
                    : (request.SelectedColumns ?? new List<string>())
                        .Where(c => !string.IsNullOrWhiteSpace(c)).ToList();

                // ── KEYS ──
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim,
                    nameof(request.SelectedColumns), nameof(request.VisualReport));

                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}_{ReportCacheKey.Columns(selectedColumns)}";

                // HTML hit: VIEW or export, no DB and no Razor
                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // ── HEADER: cached per branch, so the header query and logo conversion run once, not on every data miss ──
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                    branchIdForHeader = request.BranchIds;

                var headerTask = _dataCache.GetOrLoadAsync($"Header_{branchIdForHeader ?? "ALL"}", async () =>
                {
                    var headerData = await _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");
                    var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                    await ReportUtils.ConvertUniqueImagesToBase64Async(
                        headerData, nameof(CommonHeader.CompanyLogo), webRoot);
                    return headerData;
                }, ct);

                // ── DATA: shared by visual, normal and every column choice ──
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request, userId);
                    return loaded.Rows.Any() ? loaded : null;              // empty results are never cached
                }, ct);

                await Task.WhenAll(headerTask, dataTask);
                // header + data run together
                var headerRows = await headerTask;
                var data = await dataTask;

                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                // ── RENDER ──
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
                    { "HeaderDataSet", headerRows! },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "StatusName", data.StatusName ?? "All" },
                    { "SelectedColumns", selectedColumns },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanAllDetailsReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanAllDetialsReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);


                var result = await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Rows.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);



                return result;
            }
            catch (Exception ex)
            {

                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}


