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
    public class OfficeProgressController : ControllerBase
    {
        private const string ReportName = "OfficeProgress";

        private readonly IOfficeProgress _officeProgressService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<OfficeProgressController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public OfficeProgressController(
            IOfficeProgress officeProgressService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<OfficeProgressController> logger,
            IReportFileResponse reportFileResponse)
        {
            _officeProgressService = officeProgressService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost("GenerateReport")]
        public async Task<IActionResult> GenerateReport(
            [FromBody] OfficeProgressRequest request,
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
                        var officeProgress = await _officeProgressService.GetOfficeProgress(request);
                        return officeProgress.Rows.Any() ? officeProgress : null;
                    },
                    ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "GrandTotal", data.GrandTotal },
                    { "HeaderDataSet", await headerTask },
                    { "TotalRecords", data.Rows.Count },
                    { "TillDate", request.TillDate },
                    { "BranchName", request.BranchName },
                    { "ReportType", request.ReportType },
                    { "ProvisionType", request.ProvisionType },
                    { "Enable1to30Days", request.Enable1to30Days },
                    { "GroupByBranch", request.GroupByBranch },
                    { "GroupByCollectionCenter", request.GroupByCollectionCenter },
                    { "ViewDetail", request.ViewDetail },
                    { "Format", upperFormat },
                    { "SameCompanyName", request.SameCompanyName },
                    { "VisualReport", request.VisualReport },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/VOfficeProgressReport.cshtml"
                    : "Views/Report/Account/AccountingReport/OfficeProgress.cshtml";

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