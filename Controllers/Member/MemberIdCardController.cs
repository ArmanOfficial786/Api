//using JsSampleReport.Dtos.ReportDtos;
//using JsSampleReport.Dtos.RequestDtos;
//using JsSampleReport.Inteface.ReportInterface;
//using JsSampleReport.Inteface.ServiceInterface;
//using JsSampleReport.Utils.Report;
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

//        public MemberIdCardController(
//            IMemberIdCard memberIdCardService,
//            IMemberDetail memberDetail,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<MemberIdCardController> logger)
//        {
//            _memberIdCardService = memberIdCardService;
//            _memberDetail = memberDetail;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _reportSettings = reportSettings;
//            _logger = logger;
//        }

//        [HttpPost("MemberIdCard")]
//        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GetMemberIdCardData(
//            [FromBody] MemberIdCardRequest request,
//            [FromQuery] string format = "VIEW")
//        {

//            try
//            {
//                var response = new GeneralResponse<ReportResponseDtos>();
//                if (request == null || !ModelState.IsValid)
//                {
//                    response.IsValid = false;
//                    response.StatusCode = 400;
//                    response.Message = "Invalid request";
//                    return BadRequest(response);
//                }
//                    //return BadRequest(new { success = false, message = "Invalid request" });

//                var upperFormat = format.ToUpper();
//                var reportKey = ReportUtils.GenerateReportKey(request, "MemberIdCard");

//                ReportExportHelper.LogCacheState(
//                    upperFormat, reportKey,
//                    _jsReportService.IsHtmlCached(reportKey), _logger);

//                // ── EXPORT PATH — no DB call ──────────────────────────
//                if (upperFormat != "VIEW" && _jsReportService.IsHtmlCached(reportKey))
//                {
//                    _logger.LogInformation("✅ NO DB CALL — serving from cache");
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        reportKey, upperFormat,
//                        "MemberIdCardReport",
//                        _jsReportService, _logger);
//                }

//                var webRoot = ReportUtils.GetWebRootPath(
//                    _webHostEnvironment, _reportSettings, _logger);

//                // ════════════════════════════════════════════════════════
//                // STAGE 1 — DB queries concurrently
//                //           Member data + Header data
//                // ════════════════════════════════════════════════════════


//                // ✅ Start both DB calls — no await yet
//                var memberTask = _memberIdCardService.GetMemberIdCardData(request);
//                var headerTask = _memberDetail.GetCommonHeaders();

//                // ✅ Await both DB calls together
//                await Task.WhenAll(memberTask, headerTask);

//                var memberIdCardData = memberTask.Result;
//                var headerData = headerTask.Result;




//                if (!memberIdCardData.Any())
//                    return NotFound(new { success = false, message = "No data found" });

//                // ════════════════════════════════════════════════════════
//                // STAGE 2 — Read all COMMON images ONCE concurrently
//                //           CompanyLogo + UserSignature + AuthSignature
//                //           All 3 are shared across all ID cards
//                // ════════════════════════════════════════════════════════


//                // ✅ Get logo path from DB header
//                var header = headerData.FirstOrDefault();
//                var companyLogoPath = header?.CompanyLogo ?? "";

//                // ✅ Start all 3 common image reads concurrently — each read ONCE
//                var logoTask = ReportUtils.ReadCommonImageAsBase64Async(
//                                       webRoot, companyLogoPath, _logger);

//                // ✅ Static for now — swap with header path when comes from DB:
//                //    header?.UserSignature ?? ""
//                //    header?.AuthSignature ?? ""
//                var userSignTask = ReportUtils.ReadCommonImageAsBase64Async(
//                                       webRoot, "ArmanSignature.png", _logger);
//                var authSignTask = ReportUtils.ReadCommonImageAsBase64Async(
//                                       webRoot, "AuthSignature.png", _logger);

//                // ✅ Await all 3 together
//                await Task.WhenAll(logoTask, userSignTask, authSignTask);

//                var companyLogoBase64 = logoTask.Result;
//                var userSignatureBase64 = userSignTask.Result;
//                var authSignatureBase64 = authSignTask.Result;



//                // ════════════════════════════════════════════════════════
//                // STAGE 3 — Assign common images (zero file I/O)
//                //         + Convert member photos (unique per member)
//                // ════════════════════════════════════════════════════════


//                // ✅ Assign logo to header — string assignment only, no disk read
//                if (header != null)
//                    header.CompanyLogo = companyLogoBase64;

//                // ✅ Assign signatures to every member — string assignment only

//                foreach (var member in memberIdCardData)
//                {
//                    member.UserSignature = userSignatureBase64;
//                    member.AuthSignature = authSignatureBase64;
//                }

//                // ✅ Only MemberPhoto is unique per member — needs per-item conversion
//                await ReportUtils.ConvertUniqueImagesToBase64Async(
//                    memberIdCardData,
//                    nameof(MemberIdCardModel.MemberPhoto),
//                    webRoot, _logger);


//                // ════════════════════════════════════════════════════════
//                // STAGE 4 — Razor render
//                // ════════════════════════════════════════════════════════

//                var reportData = new Dictionary<string, object>
//                {
//                    { "MemberIdCardDataSet", memberIdCardData       },
//                    { "HeaderDataSet",       headerData             },
//                    { "TotalRecords",        memberIdCardData.Count }
//                };

//                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                    reportKey: reportKey,
//                    reportPath: "Views/Report/MemberIdCard.cshtml",
//                    data: reportData);

//                // ════════════════════════════════════════════════════════
//                // STAGE 5 — PDF generation
//                // ════════════════════════════════════════════════════════

//                var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF");

//                if (upperFormat == "VIEW")
//                {
//                    //return Ok(new
//                    //{
//                    //    success = true,
//                    //    pdfData = Convert.ToBase64String(pdfBytes),
//                    //    reportName = "MemberIdCard Report",
//                    //    // ✅ Static pagination (temporary)
//                    //    pagination = new
//                    //    {
//                    //        currentPage = request.currentPage,
//                    //        totalPages = 1,
//                    //        totalRecord = memberIdCardData.Count,
//                    //        pageSize = request.pageSize,
//                    //        hasNextPage = false,
//                    //        hasPreviousPage = false
//                    //    }
//                    //});

//                    response.IsValid = true;
//                    response.StatusCode = 200;
//                    response.Message = "Success";
//                    response.Data = new ReportResponseDtos
//                    {
//                        PdfData = Convert.ToBase64String(pdfBytes),
//                        ReportName = "MemberIdCard Report",
//                        Pagination = new Pagination
//                        {
//                            CurrentPage = request.currentPage,  
//                            TotalPages = 1,
//                            TotalRecord = memberIdCardData.Count,
//                            PageSize = request.pageSize,       
//                            HasNextPage = false,
//                            HasPreviousPage = false
//                        }
//                    };
//                    return Ok(response);
//                }


//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat,
//                    "MemberDetailReport",
//                    _jsReportService, _logger);
//            }
//            catch (Exception ex)
//            {

//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "An error occurred.",
//                    error = ex.Message
//                });
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
using System.Text.Json;

namespace JsSampleReport.Controllers.Member
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

        // ══════════════════════════════════════════════════════════════════════════
        // POST api/MemberIdCard/MemberIdCard?format=VIEW|PDF|WORD|XLSX|PNG
        //
        // VIEW   → returns binary PDF blob
        //          Content-Type: application/pdf
        //          Content-Disposition: inline
        //          X-Pagination: { currentPage, totalPages, ... }  (JSON header)
        //          ✅ ~33% smaller than base64 JSON — 48 MB → 36 MB
        //
        // EXPORT → returns binary blob
        //          Content-Type: application/pdf | docx | xlsx | png
        //          Content-Disposition: attachment; filename="MemberIdCardReport_20260407.pdf"
        // ══════════════════════════════════════════════════════════════════════════
        [HttpPost("MemberIdCard")]
        public async Task<ActionResult> GetMemberIdCardData(
            [FromBody] MemberIdCardRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request" });

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberIdCard") + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.IsHtmlCached(reportKey), _logger);

                // ── EXPORT — cache hit → skip DB entirely ─────────────────────────
                if (upperFormat != "VIEW" && _jsReportService.IsHtmlCached(reportKey))
                {
                    _logger.LogInformation("✅ NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        "MemberIdCardReport",
                        _jsReportService, _logger);
                }

                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

                // ── STAGE 1: DB queries — concurrent ──────────────────────────────
                var memberTask = _memberIdCardService.GetMemberIdCardData(request);
                var headerTask = _memberDetail.GetCommonHeaders();
                await Task.WhenAll(memberTask, headerTask);

                var memberIdCardData = memberTask.Result;
                var headerData = headerTask.Result;

                if (!memberIdCardData.Any())
                    return NotFound(new { success = false, message = "No data found" });

                // ── STAGE 2: Common images — read once, concurrent ─────────────────
                var header = headerData.FirstOrDefault();
                var companyLogoPath = header?.CompanyLogo ?? "";

                var logoTask = ReportUtils.ReadCommonImageAsBase64Async(webRoot, companyLogoPath, _logger);
                var userSignTask = ReportUtils.ReadCommonImageAsBase64Async(webRoot, "ArmanSignature.png", _logger);
                var authSignTask = ReportUtils.ReadCommonImageAsBase64Async(webRoot, "AuthSignature.png", _logger);
                await Task.WhenAll(logoTask, userSignTask, authSignTask);

                if (header != null)
                    header.CompanyLogo = logoTask.Result;

                foreach (var member in memberIdCardData)
                {
                    member.UserSignature = userSignTask.Result;
                    member.AuthSignature = authSignTask.Result;
                }

                // ── STAGE 3: Per-member unique images ─────────────────────────────
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    memberIdCardData,
                    nameof(MemberIdCardModel.MemberPhoto),
                    webRoot, _logger);

                // ── STAGE 4: Razor render + cache HTML ────────────────────────────
                var reportData = new Dictionary<string, object>
                {
                    { "MemberIdCardDataSet", memberIdCardData       },
                    { "HeaderDataSet",       headerData             },
                    { "TotalRecords",        memberIdCardData.Count },
                };

                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/MemberIdCard.cshtml",
                    data: reportData);

                // ── STAGE 5: PDF generation ───────────────────────────────────────
             

                // ── VIEW — return binary PDF blob ─────────────────────────────────
                // ✅ No base64 — raw bytes only — ~33% smaller than JSON base64
                // ✅ Pagination sent in X-Pagination header (tiny JSON, not the body)
                // ✅ Content-Disposition: inline — browser renders, does not download
                // Frontend: fetch → stream → Blob → URL.createObjectURL → <iframe>
                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                 htmlContent, "PDF", reportKey);
                    var pagination = new
                    {
                        currentPage = request.currentPage,
                        totalPages = 1,
                        totalRecord = memberIdCardData.Count,
                        pageSize = request.pageSize,
                        hasNextPage = false,
                        hasPreviousPage = false,
                    };

                    // ✅ Expose X-Pagination so frontend fetch() can read it
                    Response.Headers.Append(
                        "X-Pagination",
                        JsonSerializer.Serialize(pagination));

                    // ✅ Expose the header to fetch() on the frontend (CORS)
                    Response.Headers.Append(
                        "Access-Control-Expose-Headers",
                        "X-Pagination");

                    // ✅ inline → browser renders PDF (not download prompt)
                    Response.Headers.Append(
                        "Content-Disposition",
                        "inline; filename=\"MemberIdCardReport.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                // ── EXPORT — return binary blob with attachment header ─────────────
                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    "MemberIdCardReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in MemberIdCard");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred.",
                    error = ex.Message,
                });
            }
        }
    }
}