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
    public class CollectorWiseLoanAnalysisController : ControllerBase
    {
        private const string ReportName = "CollectorWiseLoanAnalysis";

        private readonly ICollectorWiseLoanAnalysisRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<CollectorWiseLoanAnalysisController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: A4 landscape (17 columns); visual: same page as the other visual reports
        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: false);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public CollectorWiseLoanAnalysisController(
            ICollectorWiseLoanAnalysisRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<CollectorWiseLoanAnalysisController> logger,
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
            [FromBody] CollectorWiseLoanAnalysisRequestDto request,
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
                    var loaded = await _repository.GetSummaryReportDataAsync(request);
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
                    { "TotalOpeningBalance", data.TotalOpeningBalance },
                    { "TotalOpeningActiveCount", data.TotalOpeningActiveCount },
                    { "TotalDisburseAmount", data.TotalDisburseAmount },
                    { "TotalPaymentAmount", data.TotalPaymentAmount },
                    { "TotalBalanceAmount", data.TotalBalanceAmount },
                    { "TotalBalanceActiveCount", data.TotalBalanceActiveCount },
                    { "TotalClosingBalance", data.TotalClosingBalance },
                    { "TotalClosingActiveCount", data.TotalClosingActiveCount },
                    { "TotalGoodAmt", data.TotalGoodAmt },
                    { "TotalDueAmt", data.TotalDueAmt },
                    { "TotalDue1To365", data.TotalDue1To365 },
                    { "TotalDueGt365", data.TotalDueGt365 },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", data.FromDateBs ?? "" },
                    { "ToDate", data.ToDateBs ?? "" },
                    { "FromDateAd", data.FromDateAd ?? "" },
                    { "ToDateAd", data.ToDateAd ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VCollectorWiseLoanAnalysisReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/CollectorWiseLoanAnalysisReport.cshtml";

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