using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.IBTReports;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.IBTReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IBTStatementBranchwiseController : ControllerBase
    {
        private const string ReportName = "IBTStatementBranchwise";

        private readonly IIBTStatementBranchwiseRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<IBTStatementBranchwiseController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: A4 portrait (5 or 7 columns depending on the report type); visual: same page as every visual report
        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public IBTStatementBranchwiseController(
            IIBTStatementBranchwiseRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<IBTStatementBranchwiseController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        // POST api/IBTStatementBranchwise?format=VIEW
        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] IBTStatementBranchwiseRequestDto request,
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

                // no office in the request: use the office of the logged-in user (must happen before the cache key is built)
                if (string.IsNullOrEmpty(request.OfficeId))
                {
                    var officeClaim = User.FindFirst("LoginOfficeId")?.Value;
                    if (!string.IsNullOrEmpty(officeClaim))
                        request.OfficeId = officeClaim;
                }

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                // the DB data is cached once and shared by the normal and the visual view
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // the header always shows the selected office
                var branchIdForHeader = request.OfficeId ?? "";

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null || loaded.TotalRecords == 0)
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "DetailRows", data.DetailRows },
                    { "LedgerGroupRows", data.LedgerGroupRows },
                    { "InterestRows", data.InterestRows },
                    { "TotalRecords", data.TotalRecords },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDateBs ?? "" },
                    { "ToDate", request.ToDateBs ?? "" },
                    { "LoginBranchName", data.LoginBranchName ?? "" },
                    { "PayableBranchName", data.PayableBranchName ?? "" },
                    { "ReportType", data.ReportType },
                    { "InterestRate", data.InterestRate },
                    { "MinimumClosingBalance", data.MinimumClosingBalance },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                // NOTE: the visual view below must exist before a request with VisualReport = true is made
                var viewPath = isVisual
                    ? "Views/VisualReport/VIBTStatementBranchwiseReport.cshtml"
                    : "Views/Report/Account/IBTReports/IBTStatementBranchwiseReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.TotalRecords,
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