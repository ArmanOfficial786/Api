using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Share
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShareDetailsController : ControllerBase
    {
        private const string ReportName = "ShareDetails";

        private readonly IShareDetails _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<ShareDetailsController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // normal report: 26 columns, so a wide landscape sheet
        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(500, 297, PageUnit.mm, landscape: true);

        // visual report
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public ShareDetailsController(
            IShareDetails repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<ShareDetailsController> logger,
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
            [FromBody] ShareDetailsRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userScope = User.FindFirst("UserId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userScope) || !long.TryParse(userScope, out _))
                    return Unauthorized(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                if (string.IsNullOrEmpty(request.OfficeIds))
                    return BadRequest(new { success = false, StatusCode = 400, message = "select Branch Office" });

                // normalise before the cache key is built, so "english" and "ENGLISH" share one entry
                request.ReportType = string.IsNullOrWhiteSpace(request.ReportType)
                    ? "ENGLISH"
                    : request.ReportType.ToUpperInvariant();

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

                var headerTask = _headers.GetAsync(null, ct);

                var dataTask = _dataCache.GetOrLoadAsync(
                    dataKey,
                    async () =>
                    {
                        var shareDetails = await _repository.GetReportDataAsync(request);
                        return shareDetails.Rows.Any() ? shareDetails : null;
                    },
                    ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalMembers", data.TotalMembers },
                    { "TotalShare", data.TotalShare },
                    { "TotalShareAmount", data.TotalShareAmount },
                    { "HeaderDataSet", await headerTask },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "ShareTypeName", data.ShareTypeName ?? "All" },
                    { "MemberTypeName", data.MemberTypeName ?? "All" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "All" },
                    { "EnableCollectionCenter", request.EnableCollectionCenter },
                    { "EnableGroup", request.EnableGroup },
                    { "ReportType", data.ReportType ?? "ENGLISH" },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat },
                    { "VisualReport", isVisual },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/VShareDetailsReport.cshtml"
                    : "Views/Report/Share/ShareDetailsReport.cshtml";

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