using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ReportInterface;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Utils;
using JsSampleReport.Utils.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JsSampleReport.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberDetailController : ControllerBase
    {
        private readonly IMemberDetail _memberDetail;
        private readonly IJsReportService _jsReportService;
        private readonly ILogger<MemberDetailController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;

        public MemberDetailController(
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            ILogger<MemberDetailController> logger,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings)
        {
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
        }

        [HttpPost("generate-report")]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberDetailRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request" });

                var upperFormat = format.ToUpper();

                // ✅ Key from utils — works with any request type
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberReport");

                // ✅ Debug log from utils
                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.IsCached(reportKey), _logger);

                // ── EXPORT PATH ───────────────────────────────────────────
                if (upperFormat != "VIEW" && _jsReportService.IsCached(reportKey))
                {
                    _logger.LogInformation("✅ NO DB CALL — serving from server cache");
                    return ReportExportHelper.ExportFromCache(
                        reportKey, upperFormat,
                        "MemberDetailReport",
                        _jsReportService, _logger);
                }

                // ── VIEW PATH ─────────────────────────────────────────────
                _logger.LogInformation("🔄 DB CALL — fetching fresh data");

                var allMemberData = await _memberDetail.GetMemberRegistrationDetail(request);

                if (allMemberData == null || !allMemberData.Any())
                    return NotFound(new { success = false, message = "No data found" });

                var headerData = await _memberDetail.GetCommonHeaders();

                // ✅ WebRootPath resolved from appsettings.json
                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

                //ReportUtils.ConvertLogoToBase64(headerData, webRoot, _logger);
                await ReportUtils.ConvertUniqueImagesToBase64Async(headerData, nameof(CommonHeader.CompanyLogo), webRoot, _logger);

                var reportData = new Dictionary<string, object>
                {
                    { "StudentDataSet", allMemberData },
                    { "HeaderDataSet",  headerData },
                    { "TotalRecords",   allMemberData.Count() }
                };

                var htmlContent = _jsReportService.RenderAndCacheReport(
                    reportKey: reportKey,
                    reportPath: "Views/Report/MemberReport.cshtml",
                    data: reportData
                );

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = _jsReportService.GenerateReportFromHtml(htmlContent, "PDF");
                    return Ok(new
                    {
                        success = true,
                        pdfData = Convert.ToBase64String(pdfBytes),
                        reportName = "Member Detail Report"
                    });
                }

                return ReportExportHelper.ExportFromCache(
                    reportKey, upperFormat,
                    "MemberDetailReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in generate-report");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}