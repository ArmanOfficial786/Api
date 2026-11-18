using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.CenterDetailReports;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Microfinance.CenterDetailReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CollectorWiseProjectionController : ControllerBase
    {
        private const string ReportName = "CollectorWiseProjection";

        private readonly ICollectorWiseProjectionRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<CollectorWiseProjectionController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal: 280 x 297 mm portrait (this report's existing page, 23 narrow columns); visual: same page as every visual report
        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(280, 297, PageUnit.mm, landscape: false);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public CollectorWiseProjectionController(
            ICollectorWiseProjectionRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<CollectorWiseProjectionController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        // POST api/CollectorWiseProjection?format=VIEW
        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] CollectorWiseProjectionRequestDto request,
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

                // the header is the company header (no branch), as in the old controller
                var headerTask = _headers.GetAsync("", ct);
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

                    { "TotalMemberProjection", data.TotalMemberProjection },
                    { "TotalMThisMAdd", data.TotalMThisMAdd },
                    { "TotalMUptoThisMAdd", data.TotalMUptoThisMAdd },
                    { "TotalMUptoThisMRemove", data.TotalMUptoThisMRemove },
                    { "TotalLoanProjection", data.TotalLoanProjection },
                    { "TotalLUptoThisMAdd", data.TotalLUptoThisMAdd },
                    { "TotalLUptoThisMProjected", data.TotalLUptoThisMProjected },
                    { "TotalBalanceLoan", data.TotalBalanceLoan },
                    { "TotalCenters", data.TotalCenters },
                    { "TotalMembers", data.TotalMembers },

                    { "FiscalYear", data.FiscalYear ?? "" },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "TillDateAd", data.TillDateAd ?? "" },
                    { "MonthCode", data.MonthCode ?? "" },
                    { "PreviousMonthStartDate", data.PreviousMonthStartDate ?? "" },
                    { "PreviousMonthLastDate", data.PreviousMonthLastDate ?? "" },
                    { "CurrentMonthStartDate", data.CurrentMonthStartDate ?? "" },
                    { "CurrentFiscalYearStartDate", data.CurrentFiscalYearStartDate ?? "" },
                    { "BranchName", data.BranchName ?? "All Branch" },

                    { "HeaderDataSet", await headerTask },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                // NOTE: the visual view below must exist before a request with VisualReport = true is made
                var viewPath = isVisual
                    ? "Views/VisualReport/Microfinance/VCollectorWiseProjectionReport.cshtml"
                    : "Views/Report/Microfinance/CenterDetailReports/CollectorWiseProjectionReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Rows.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}