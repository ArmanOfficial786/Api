using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Member
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberIdCardController : ControllerBase
    {
        private const string ReportName = "MemberIdCard";

        private readonly IMemberIdCard _memberIdCardService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberIdCardController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // two 70 mm cards per row on A4 portrait
        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Portrait;

        public MemberIdCardController(
            IMemberIdCard memberIdCardService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberIdCardController> logger,
            IReportFileResponse reportFileResponse)
        {
            _memberIdCardService = memberIdCardService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost("MemberIdCard")]
        public async Task<IActionResult> GetMemberIdCardData(
            [FromBody] MemberIdCardRequest request,
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

                var dataKey = ReportCacheKey.ForData(request, ReportName, userScope);
                var reportKey = $"{dataKey}_NORMAL_{PageSetting.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, PageSetting, ct);
                if (cached != null) return cached;

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                // company header (the provider already supplies the logo as an image value)
                var headerTask = _headers.GetAsync(null, ct);

                // members with every image already converted, so the cached copy is never changed afterwards
                var dataTask = _dataCache.GetOrLoadAsync(
                    dataKey,
                    async () =>
                    {
                        var members = await _memberIdCardService.GetMemberIdCardData(request);
                        if (members == null || !members.Any()) return null;

                        // shared signature images: read once, concurrently
                        var userSignTask = ReportUtils.ReadCommonImageAsBase64Async(webRoot, "ArmanSignature.png", _logger);
                        var authSignTask = ReportUtils.ReadCommonImageAsBase64Async(webRoot, "AuthSignature.png", _logger);
                        await Task.WhenAll(userSignTask, authSignTask);

                        foreach (var member in members)
                        {
                            member.UserSignature = userSignTask.Result;
                            member.AuthSignature = authSignTask.Result;
                        }

                        // per-member photos
                        await ReportUtils.ConvertUniqueImagesToBase64Async(
                            members, nameof(MemberIdCardModel.MemberPhoto), webRoot);

                        return members;
                    },
                    ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var totalRecords = data.Count();

                var reportData = new Dictionary<string, object>
                {
                    { "MemberIdCardDataSet", data },
                    { "HeaderDataSet", await headerTask },
                    { "TotalRecords", totalRecords },
                    { "Format", upperFormat },
                    { "PageSetting", PageSetting }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/Member/MemberIdCard.cshtml",
                    data: reportData,
                    ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: totalRecords,
                    _jsReportService, _reportFileResponse, _logger, PageSetting, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}