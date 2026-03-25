//using JsSampleReport.Dtos.ReportDtos;
//using JsSampleReport.Dtos.RequestDtos;
//using JsSampleReport.Inteface.ReportInterface;
//using JsSampleReport.Inteface.ServiceInterface;
//using JsSampleReport.Utils.Report;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;

//namespace JsSampleReport.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class MemberIdCardController : ControllerBase
//    {
//        private readonly IMemberIdCard _memberIdCardService;
//        private readonly IMemberDetail _memberDetail;
//        private readonly IJsReportService _jsReportService;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<MemberIdCardController> _logger;

//        public MemberIdCardController(IMemberIdCard memberIdCardService, IJsReportService jsReportService, IWebHostEnvironment webHostEnvironment, IOptions<ReportSettings> reportSettings, ILogger<MemberIdCardController> logger, IMemberDetail memberDetail)
//        {
//            _memberIdCardService = memberIdCardService;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _memberDetail = memberDetail;
//        }

//        [HttpPost("MemberIdCard")]
//        public IActionResult  GetMemberIdCardData(MemberIdCardRequest request, string format = "VIEW")
//        {
//            try
//            {
//                if (request == null || !ModelState.IsValid)
//                    return BadRequest(new { success = false, message = "Invalid request" });

//                var upperFormat = format.ToUpper();

//                // ✅ Key from utils — works with any request type
//                var reportKey = ReportUtils.GenerateReportKey(request, "MemberReport");

//                // ✅ Debug log from utils
//                ReportExportHelper.LogCacheState(
//                    upperFormat, reportKey,
//                    _jsReportService.IsCached(reportKey), _logger);

//                // ── EXPORT PATH ───────────────────────────────────────────
//                if (upperFormat != "VIEW" && _jsReportService.IsCached(reportKey))
//                {
//                    _logger.LogInformation("✅ NO DB CALL — serving from server cache");
//                    return ReportExportHelper.ExportFromCache(
//                        reportKey, upperFormat,
//                        "MemberIdCardReport",
//                        _jsReportService, _logger);
//                }

//                // ── VIEW PATH ─────────────────────────────────────────────
//                _logger.LogInformation("🔄 DB CALL — fetching fresh data");
//                var memberIdCardData = _memberIdCardService.GetMemberIdCardData(request).ToList();
//                var headerData = _memberDetail.GetCommonHeaders();
//                // ✅ WebRootPath resolved from appsettings.json
//                var webRoot = ReportUtils.GetWebRootPath(
//                    _webHostEnvironment, _reportSettings, _logger);

//                //ReportUtils.ConvertLogoToBase64(headerData, webRoot, _logger);

//                ReportUtils.ConvertImagesToBase64(headerData, nameof(CommonHeader.CompanyLogo), webRoot, _logger);
//                ReportUtils.ConvertImagesToBase64(memberIdCardData, nameof(MemberIdCardResponseModel.MemberPhoto), webRoot, _logger);


//                // ✅ Step 1 — Set static file paths FIRST (they are null from DB)
//                foreach (var member in memberIdCardData)
//                {
//                    member.UserSignature = "ArmanSignature.png";
//                    member.AuthSignature = "AuthSignature.png";
//                }

//                // ✅ Convert both static signature images to base64
//                ReportUtils.ConvertImagesToBase64(memberIdCardData,nameof(MemberIdCardResponseModel.UserSignature),webRoot, _logger);
//                ReportUtils.ConvertImagesToBase64(memberIdCardData,nameof(MemberIdCardResponseModel.AuthSignature),webRoot, _logger);



//                var reportData = new Dictionary<string, object>
//                {
//                    { "MemberIdCardDataSet", memberIdCardData },
//                    { "HeaderDataSet",  headerData },
//                    { "TotalRecords",   memberIdCardData.Count() }
//                };

//                var htmlContent = _jsReportService.RenderAndCacheReport(
//                    reportKey: reportKey,
//                    reportPath: "Views/Report/MemberIdCard.cshtml",
//                    data: reportData
//                );

//                if (upperFormat == "VIEW")
//                {
//                    var pdfBytes = _jsReportService.GenerateReportFromHtml(htmlContent, "PDF");
//                    return Ok(new
//                    {
//                        success = true,
//                        pdfData = Convert.ToBase64String(pdfBytes),
//                        reportName = "MemberIdCard Report"
//                    });
//                }

//                return ReportExportHelper.ExportFromCache(
//                    reportKey, upperFormat,
//                    "MemberIdCardReport",
//                    _jsReportService, _logger);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error occurred while fetching member ID card data.");
//                return StatusCode(500, "An error occurred while processing your request.");
//            }
//        }
//    }
//}








using JsSampleReport.Dtos.ReportDtos;
using JsSampleReport.Dtos.RequestDtos;
using JsSampleReport.Inteface.ReportInterface;
using JsSampleReport.Inteface.ServiceInterface;
using JsSampleReport.Utils.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

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

        public MemberIdCardController(
            IMemberIdCard memberIdCardService,
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberIdCardController> logger)
        {
            _memberIdCardService = memberIdCardService;
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost("MemberIdCard")]
        public async Task<IActionResult> GetMemberIdCardData(
            [FromBody] MemberIdCardRequest request,
            [FromQuery] string format = "VIEW")
        {

            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request" });

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberIdCard");

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.IsCached(reportKey), _logger);

                // ── EXPORT PATH — no DB call ──────────────────────────
                if (upperFormat != "VIEW" && _jsReportService.IsCached(reportKey))
                {
                    _logger.LogInformation("✅ NO DB CALL — serving from cache");
                    return ReportExportHelper.ExportFromCache(
                        reportKey, upperFormat,
                        "MemberIdCardReport",
                        _jsReportService, _logger);
                }

                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

                // ════════════════════════════════════════════════════════
                // STAGE 1 — DB queries concurrently
                //           Member data + Header data
                // ════════════════════════════════════════════════════════
                

                // ✅ Start both DB calls — no await yet
                var memberTask = _memberIdCardService.GetMemberIdCardData(request);
                var headerTask = _memberDetail.GetCommonHeaders();

                // ✅ Await both DB calls together
                await Task.WhenAll(memberTask, headerTask);

                var memberIdCardData = memberTask.Result;
                var headerData = headerTask.Result;

               
            

                if (!memberIdCardData.Any())
                    return NotFound(new { success = false, message = "No data found" });

                // ════════════════════════════════════════════════════════
                // STAGE 2 — Read all COMMON images ONCE concurrently
                //           CompanyLogo + UserSignature + AuthSignature
                //           All 3 are shared across all ID cards
                // ════════════════════════════════════════════════════════
               

                // ✅ Get logo path from DB header
                var header = headerData.FirstOrDefault();
                var companyLogoPath = header?.CompanyLogo ?? "";

                // ✅ Start all 3 common image reads concurrently — each read ONCE
                var logoTask = ReportUtils.ReadCommonImageAsBase64Async(
                                       webRoot, companyLogoPath, _logger);

                // ✅ Static for now — swap with header path when comes from DB:
                //    header?.UserSignature ?? ""
                //    header?.AuthSignature ?? ""
                var userSignTask = ReportUtils.ReadCommonImageAsBase64Async(
                                       webRoot, "ArmanSignature.png", _logger);
                var authSignTask = ReportUtils.ReadCommonImageAsBase64Async(
                                       webRoot, "AuthSignature.png", _logger);

                // ✅ Await all 3 together
                await Task.WhenAll(logoTask, userSignTask, authSignTask);

                var companyLogoBase64 = logoTask.Result;
                var userSignatureBase64 = userSignTask.Result;
                var authSignatureBase64 = authSignTask.Result;

             

                // ════════════════════════════════════════════════════════
                // STAGE 3 — Assign common images (zero file I/O)
                //         + Convert member photos (unique per member)
                // ════════════════════════════════════════════════════════
             

                // ✅ Assign logo to header — string assignment only, no disk read
                if (header != null)
                    header.CompanyLogo = companyLogoBase64;

                // ✅ Assign signatures to every member — string assignment only
                
                foreach (var member in memberIdCardData)
                {
                    member.UserSignature = userSignatureBase64;
                    member.AuthSignature = authSignatureBase64;
                }

                // ✅ Only MemberPhoto is unique per member — needs per-item conversion
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    memberIdCardData,
                    nameof(MemberIdCardResponseModel.MemberPhoto),
                    webRoot, _logger);


                // ════════════════════════════════════════════════════════
                // STAGE 4 — Razor render
                // ════════════════════════════════════════════════════════

                var reportData = new Dictionary<string, object>
                {
                    { "MemberIdCardDataSet", memberIdCardData       },
                    { "HeaderDataSet",       headerData             },
                    { "TotalRecords",        memberIdCardData.Count }
                };

                var htmlContent = _jsReportService.RenderAndCacheReport(
                    reportKey: reportKey,
                    reportPath: "Views/Report/MemberIdCard.cshtml",
                    data: reportData);

                // ════════════════════════════════════════════════════════
                // STAGE 5 — PDF generation
                // ════════════════════════════════════════════════════════

                var pdfBytes = _jsReportService.GenerateReportFromHtml(htmlContent, "PDF");

                if (upperFormat == "VIEW")
                {
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
             
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred.",
                    error = ex.Message
                });
            }
        }
    }
}