using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.OtherReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PEARLSAnalysisController : ControllerBase
    {
        private const string ReportName = "PEARLSAnalysis";

        private readonly IPEARLSAnalysis _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<PEARLSAnalysisController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: A4 portrait (6 columns); visual: same page as every visual report
        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public PEARLSAnalysisController(
            IPEARLSAnalysis repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<PEARLSAnalysisController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        // POST api/PEARLSAnalysis?format=VIEW
        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] PEARLSAnalysisRequestDto request,
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

                // the DB data is cached once and shared by the normal and the visual view
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // the header shows one branch only when a single branch is selected
                var branchIdForHeader = "";
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    !request.BranchId.Contains(',') &&
                    long.TryParse(request.BranchId, out _))
                {
                    branchIdForHeader = request.BranchId;
                }

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetPEARLSAnalysisDataAsync(request);
                    if (loaded == null ||
                        (!loaded.PEARLSP.Any() && !loaded.PEARLSE.Any() && !loaded.PEARLSA.Any() &&
                         !loaded.PEARLSR.Any() && !loaded.PEARLSL.Any() && !loaded.PEARLSS.Any()))
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "PEARLSP", data.PEARLSP },
                    { "PEARLSE", data.PEARLSE },
                    { "PEARLSA", data.PEARLSA },
                    { "PEARLSR", data.PEARLSR },
                    { "PEARLSL", data.PEARLSL },
                    { "PEARLSS", data.PEARLSS },
                    { "TillDate", data.TillDate ?? "" },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "FiscalYear", data.FiscalYear ?? "" },
                    { "PreviousDate", data.PreviousDate },
                    { "HeaderDataSet", await headerTask },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                // NOTE: the visual view below must exist before a request with VisualReport = true is made
                var viewPath = isVisual
                    ? "Views/VisualReport/VPEARLSAnalysisReport.cshtml"
                    : "Views/Report/Account/OtherReports/PEARLSAnalysisReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                var totalRecords = data.PEARLSP.Count + data.PEARLSE.Count + data.PEARLSA.Count +
                                   data.PEARLSR.Count + data.PEARLSL.Count + data.PEARLSS.Count;

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
    }
}