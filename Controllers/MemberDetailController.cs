
////============This is exactly how SSRS, Crystal Reports, FastReport work internally — render once, export many times from server-side cache.=========================


//using System.Security.Cryptography;
//using System.Text;
//using System.Text.Json;
//using JsSampleProject.Dtos.MemberDtos;
//using JsSampleProject.Interface;
//using JsSampleReport.Dtos.ReportDtos;
//using JsSampleReport.Inteface.ReportInterface;
//using Microsoft.AspNetCore.Mvc;

//namespace JsSampleReport.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class MemberDetailController : ControllerBase
//    {
//        private readonly IMemberDetail _memberDetail;
//        private readonly IJsReportService _jsReportService;
//        private readonly ILogger<MemberDetailController> _logger;
//        private readonly IWebHostEnvironment _webHostEnvironment;

//        public MemberDetailController(
//            IMemberDetail memberDetail,
//            IJsReportService jsReportService,
//            ILogger<MemberDetailController> logger,
//            IWebHostEnvironment webHostEnvironment)
//        {
//            _memberDetail = memberDetail;
//            _jsReportService = jsReportService;
//            _logger = logger;
//            _webHostEnvironment = webHostEnvironment;
//        }

//        // ✅ Single endpoint — handles VIEW + all exports internally
//        [HttpPost("generate-report")]
//        public IActionResult GenerateReport(
//            [FromBody] MemberDetailRequest request,
//            [FromQuery] string format = "VIEW")
//        {
//            try
//            {
//                if (request == null || !ModelState.IsValid)
//                    return BadRequest(new { success = false, message = "Invalid request" });

//                var upperFormat = format.ToUpper();

//                // ✅ Deterministic key — derived from request fields
//                // Same params → same key every time, no token passed anywhere
//                var reportKey = GenerateReportKey(request);

//                // ── DEBUG LOG — remove after confirming cache works ───────────
//                _logger.LogInformation("==========================================");
//                _logger.LogInformation($"FORMAT    : {upperFormat}");
//                _logger.LogInformation($"CACHE KEY : {reportKey}");
//                _logger.LogInformation($"IS CACHED : {_jsReportService.IsCached(reportKey)}");
//                _logger.LogInformation("==========================================");

//                // ══════════════════════════════════════════════════════════════
//                // EXPORT PATH
//                // Cache exists + format is not VIEW → serve from cache, NO DB
//                // ══════════════════════════════════════════════════════════════
//                if (upperFormat != "VIEW" && _jsReportService.IsCached(reportKey))
//                {
//                    _logger.LogInformation($"✅ Export from cache: {upperFormat}");
//                    return ExportFromCache(reportKey, upperFormat);
//                }

//                // ══════════════════════════════════════════════════════════════
//                // VIEW PATH (or cache expired)
//                // Fresh DB fetch → render → cache HTML
//                // ══════════════════════════════════════════════════════════════
//                _logger.LogInformation("🔄 Fetching from DB...");

//                var allMemberData = _memberDetail.GetMemberRegistrationDetail(request);

//                if (allMemberData == null || !allMemberData.Any())
//                    return NotFound(new { success = false, message = "No data found" });

//                var headerData = _memberDetail.GetCommonHeaders();
//                ConvertLogoToBase64(headerData);

//                var reportData = new Dictionary<string, object>
//                {
//                    { "StudentDataSet", allMemberData },
//                    { "HeaderDataSet",  headerData },
//                    { "TotalRecords",   allMemberData.Count() }
//                };

//                // ✅ Render .cshtml → HTML and cache on server
//                var htmlContent = _jsReportService.RenderAndCacheReport(
//                    reportKey: reportKey,
//                    reportPath: "Views/Report/MemberReport.cshtml",
//                    data: reportData
//                );

//                // VIEW → return base64 PDF for iframe display
//                if (upperFormat == "VIEW")
//                {
//                    var pdfBytes = _jsReportService.GenerateReportFromHtml(
//                                        htmlContent, "PDF");

//                    return Ok(new
//                    {
//                        success = true,
//                        pdfData = Convert.ToBase64String(pdfBytes),
//                        reportName = "Member Detail Report"
//                        // ✅ No reportKey in response — fully internal
//                    });
//                }

//                // Export format called before VIEW (direct export)
//                return ExportFromCache(reportKey, upperFormat);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in generate-report");
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "An error occurred while generating the report",
//                    error = ex.Message
//                });
//            }
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // PRIVATE — Export from server cache (no DB, no extra endpoint)
//        // ══════════════════════════════════════════════════════════════════════
//        private IActionResult ExportFromCache(string reportKey, string format)
//        {
//            var cachedHtml = _jsReportService.GetFromCache(reportKey);

//            if (cachedHtml == null)
//            {
//                _logger.LogWarning($"❌ Cache miss on export: {reportKey}");
//                return BadRequest(new
//                {
//                    success = false,
//                    message = "Report session expired. Please view the report again."
//                });
//            }

//            _logger.LogInformation($"✅ Exporting from cache: {format}");

//            var reportBytes = _jsReportService.GenerateReportFromHtml(cachedHtml, format);
//            var (contentType, ext) = GetContentTypeAndExtension(format);
//            var fileName = $"MemberDetailReport_{DateTime.Now:yyyyMMddHHmmss}.{ext}";

//            return File(reportBytes, contentType, fileName);
//        }

//        // ══════════════════════════════════════════════════════════════════════
//        // PRIVATE — Build deterministic cache key from request fields
//        // SHA256(JSON(request)) → same params always produce the same key
//        // ══════════════════════════════════════════════════════════════════════
//        private static string GenerateReportKey(MemberDetailRequest request)
//        {
//            var json = JsonSerializer.Serialize(request,
//                            new JsonSerializerOptions { WriteIndented = false });

//            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
//            var hash = Convert.ToHexString(bytes)[..16];

//            return $"MemberReport_{hash}";
//        }

//        // ── Convert logo relative path → base64 data URL ──────────────────────
//        private void ConvertLogoToBase64(IEnumerable<CommonHeader> headers)
//        {
//            if (headers == null) return;

//            foreach (var header in headers)
//            {
//                if (string.IsNullOrWhiteSpace(header.CompanyLogo)) continue;

//                try
//                {
//                    var webRoot = _webHostEnvironment.WebRootPath
//                                  ?? @"C:\inetpub\wwwroot";

//                    var relativePath = header.CompanyLogo
//                        .Replace('/', Path.DirectorySeparatorChar)
//                        .Replace('\\', Path.DirectorySeparatorChar)
//                        .TrimStart(Path.DirectorySeparatorChar);

//                    var fullPath = Path.Combine(webRoot, relativePath);

//                    _logger.LogInformation($"Logo path: {fullPath}");

//                    if (!System.IO.File.Exists(fullPath))
//                    {
//                        _logger.LogWarning($"Logo not found: {fullPath}");
//                        header.CompanyLogo = string.Empty;
//                        continue;
//                    }

//                    var imageBytes = System.IO.File.ReadAllBytes(fullPath);
//                    var base64 = Convert.ToBase64String(imageBytes);
//                    var ext = Path.GetExtension(fullPath).TrimStart('.').ToLower();

//                    var mimeType = ext switch
//                    {
//                        "png" => "image/png",
//                        "jpg" or "jpeg" => "image/jpeg",
//                        "gif" => "image/gif",
//                        "bmp" => "image/bmp",
//                        "webp" => "image/webp",
//                        _ => "image/png"
//                    };

//                    header.CompanyLogo = $"data:{mimeType};base64,{base64}";
//                    _logger.LogInformation("✅ Logo converted to base64.");
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, $"Logo conversion failed: {header.CompanyLogo}");
//                    header.CompanyLogo = string.Empty;
//                }
//            }
//        }

//        private (string contentType, string extension)
//            GetContentTypeAndExtension(string format) => format switch
//            {
//                "VIEW" => ("application/pdf", "pdf"),
//                "HTML" => ("text/html", "html"),
//                "PDF" => ("application/pdf", "pdf"),
//                "EXCEL" or "XLSX" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
//                "WORD" or "DOCX" => ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "docx"),
//                "PNG" => ("image/png", "png"),
//                "CSV" => ("text/csv", "csv"),
//                _ => ("application/pdf", "pdf")
//            };
//    }
//}





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
        public IActionResult GenerateReport(
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

                var allMemberData = _memberDetail.GetMemberRegistrationDetail(request);

                if (allMemberData == null || !allMemberData.Any())
                    return NotFound(new { success = false, message = "No data found" });

                var headerData = _memberDetail.GetCommonHeaders();

                // ✅ WebRootPath resolved from appsettings.json
                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

                ReportUtils.ConvertLogoToBase64(headerData, webRoot, _logger);

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