using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Member
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberAllDetailsController : ControllerBase
    {
        private const string ReportName = "MemberAllDetails";
        private const string FileName = "MemberDetailReport";

        private readonly IMemberAllDetails _memberAllDetails;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<MemberAllDetailsController> _logger;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(650, 420, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public MemberAllDetailsController(
            IMemberAllDetails memberAllDetails,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<MemberAllDetailsController> logger,
            IReportFileResponse reportFileResponse,
            IOptions<ReportSettings> reportSettings,
            IWebHostEnvironment webHostEnvironment)
        {
            _memberAllDetails = memberAllDetails;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
            _reportSettings = reportSettings;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost]
        public async Task<IActionResult> GetMemberAllDetails(
            [FromBody] MemberAllDetailRequst request,
            [FromQuery] string format = "View",
            CancellationToken ct = default)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.visualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.visualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, FileName, pdfView: request.visualReport,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync("", ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _memberAllDetails.GetMemberAllDetailsAsync(request);
                    if (loaded == null || !loaded.Any())
                        return null;

                    var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                    await ReportUtils.AttachMemberPhotosAsync(
                        loaded,
                        idPropertyName: nameof(MemberAllDetailSpResponse.MemberId),
                        imagePropertyName: nameof(MemberAllDetailSpResponse.MemberImage),
                        webRoot: webRoot,
                        logger: _logger);

                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "MemberAllDetailDataSet", data },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", $"{request.fromDate}" },
                    { "ToDate", $"{request.toDate}" },
                    { "BranchName", "All" },
                    { "SelectedColumns", request.SelectedColumns ?? new List<string>() },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.visualReport
                    ? "Views/VisualReport/Member/VMemeberAllDetailReport.cshtml"
                    : "Views/Report/Member/MemberAllDetails.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: viewPath,
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, FileName, pdfView: request.visualReport,
                    totalRecords: data.Count(),
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