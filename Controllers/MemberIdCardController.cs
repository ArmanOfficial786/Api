using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ReportInterface;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Utils.Report;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JsSampleReport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberIdCardController : ControllerBase
    {
        private readonly IMemberIdCard _memberIdCardService;
        private readonly IMemberDetail _memberDetail;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberIdCardController> _logger;

        public MemberIdCardController(IMemberIdCard memberIdCardService, IJsReportService jsReportService, IWebHostEnvironment webHostEnvironment, IOptions<ReportSettings> reportSettings, ILogger<MemberIdCardController> logger, IMemberDetail memberDetail)
        {
            _memberIdCardService = memberIdCardService;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _memberDetail = memberDetail;
        }

        [HttpPost("MemberIdCard")]
        public IActionResult  GetMemberIdCardData(MemberIdCardRequest request, string format = "VIEW")
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
                        "MemberIdCardReport",
                        _jsReportService, _logger);
                }

                // ── VIEW PATH ─────────────────────────────────────────────
                _logger.LogInformation("🔄 DB CALL — fetching fresh data");
                var memberIdCardData = _memberIdCardService.GetMemberIdCardData(request);
                var headerData = _memberDetail.GetCommonHeaders();
                // ✅ WebRootPath resolved from appsettings.json
                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

                ReportUtils.ConvertLogoToBase64(headerData, webRoot, _logger);

                var reportData = new Dictionary<string, object>
                {
                    { "MemberIdCardDataSet", memberIdCardData },
                    { "HeaderDataSet",  headerData },
                    { "TotalRecords",   memberIdCardData.Count() }
                };

                var htmlContent = _jsReportService.RenderAndCacheReport(
                    reportKey: reportKey,
                    reportPath: "Views/Report/MemberIdCard.cshtml",
                    data: reportData
                );

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = _jsReportService.GenerateReportFromHtml(htmlContent, "PDF");
                    return Ok(new
                    {
                        success = true,
                        pdfData = Convert.ToBase64String(pdfBytes),
                        reportName = "MemberIdCard Report"
                    });
                }

                return ReportExportHelper.ExportFromCache(
                    reportKey, upperFormat,
                    "MemberIdCardReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching member ID card data.");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
