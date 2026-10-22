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
    public class PLAccountController : ControllerBase
    {
        private const string ReportName = "PLAccount";

        private readonly IPLAccount _plAccountService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<PLAccountController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public PLAccountController(
            IPLAccount plAccountService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<PLAccountController> logger,
            IReportFileResponse reportFileResponse)
        {
            _plAccountService = plAccountService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] PLAccountRequest request,
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
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    headerBranch = request.BranchIds;
                }
                var headerTask = _headers.GetAsync(headerBranch, ct);

                var dataTask = _dataCache.GetOrLoadAsync(
                    dataKey,
                    async () =>
                    {
                        var plAccount = await _plAccountService.GetPLAccountReport(request);
                        return HasData(plAccount) ? plAccount : null;
                    },
                    ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found." });

                var totalRecords = CountRows(data);

                var reportData = new Dictionary<string, object>
                {
                    { "PLData", data },
                    { "DisplayType", request.DisplayType },
                    { "IsNepali", request.IsNepaliReport },
                    { "HeaderDataSet", await headerTask },
                    { "TotalRecords", totalRecords },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.BranchName },
                    { "ReportType", request.ReportType },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat },
                    { "SameCompanyName", request.SameCompanyName },
                    { "VisualReport", request.VisualReport },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/VPLAccountReport.cshtml"
                    : "Views/Report/Account/AccountingReport/PLAccountReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: totalRecords,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // the service returns either the horizontal or the vertical shape
        private static bool HasData(object? data) => data switch
        {
            PLAccountHorizontalData horizontal => horizontal.IncomeRows.Any() || horizontal.ExpenseRows.Any(),
            PLAccountVerticalData vertical => vertical.Rows.Any(),
            _ => false
        };

        private static int CountRows(object? data) => data switch
        {
            PLAccountHorizontalData horizontal => horizontal.IncomeRows.Count + horizontal.ExpenseRows.Count,
            PLAccountVerticalData vertical => vertical.Rows.Count,
            _ => 0
        };
    }
}