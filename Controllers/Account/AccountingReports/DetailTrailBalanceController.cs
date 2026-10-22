using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.AccountingReports
{
    [ApiController]
    [Route("api/[controller]")]
    public class DetailTrialBalanceController : ControllerBase
    {
        private const string ReportName = "DetailTrialBalance";

        private readonly IDetailTrailBalance _detailService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<DetailTrialBalanceController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public DetailTrialBalanceController(
            IDetailTrailBalance detailService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<DetailTrialBalanceController> logger,
            IReportFileResponse reportFileResponse)
        {
            _detailService = detailService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] DetailTrialBalanceRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var userScope = User.FindFirst("UserId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous";

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userScope, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // header is branch specific only when exactly one branch is selected
                string? headerBranch = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" && !request.BranchId.Contains(','))
                {
                    headerBranch = request.BranchId;
                }
                var headerTask = _headers.GetAsync(headerBranch, ct);

                var dataTask = _dataCache.GetOrLoadAsync(
                    dataKey,
                    async () =>
                    {
                        var trialBalance = await _detailService.GetDetailTrialBalance(request);
                        return trialBalance.Rows.Any() ? trialBalance : null;
                    },
                    ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalDebit", data.TotalDebit },
                    { "TotalCredit", data.TotalCredit },
                    { "TotalAssetExpenses", data.TotalAssetExpenses },
                    { "TotalLiabilitiesIncome", data.TotalLiabilitiesIncome },
                    { "HeaderDataSet", await headerTask },
                    { "TotalRecords", data.Rows.Count },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.BranchName },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat },
                    { "SameCompanyName", request.SameCompanyName },
                    { "VisualReport", request.VisualReport },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/VDetailTrialBalanceReport.cshtml"
                    : "Views/Report/Account/AccountingReport/DetailTrailBalance.cshtml";

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