//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface;
//using NexgenCosysReport.Utils;
//using NexgenCosysReport.Utils.Report;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;

//namespace NexgenCosysReport.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class MemberDetailController : ControllerBase
//    {
//        private readonly IMemberDetail _memberDetail;
//        private readonly IJsReportService _jsReportService;
//        private readonly ILogger<MemberDetailController> _logger;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly IOptions<ReportSettings> _reportSettings;

//        public MemberDetailController(
//            IMemberDetail memberDetail,
//            IJsReportService jsReportService,
//            ILogger<MemberDetailController> logger,
//            IWebHostEnvironment webHostEnvironment,
//            IOptions<ReportSettings> reportSettings)
//        {
//            _memberDetail = memberDetail;
//            _jsReportService = jsReportService;
//            _logger = logger;
//            _webHostEnvironment = webHostEnvironment;
//            _reportSettings = reportSettings;
//        }

//        [HttpPost("generate-report")]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] MemberDetailRequest request,
//            [FromQuery] string format = "VIEW")
//        {
//            try
//            {
//                if (request == null || !ModelState.IsValid)
//                    return BadRequest(new { success = false, message = "Invalid request" });

//                var upperFormat = format.ToUpper();

//                // ? Key from utils — works with any request type
//                var reportKey = ReportUtils.GenerateReportKey(request, "MemberReport");

//                // ? Debug log from utils
//                ReportExportHelper.LogCacheState(
//                    upperFormat, reportKey,
//                    _jsReportService.IsHtmlCached(reportKey), _logger);

//                // -- EXPORT PATH -------------------------------------------
//                if (upperFormat != "VIEW" && _jsReportService.IsHtmlCached(reportKey))
//                {
//                    _logger.LogInformation("? NO DB CALL — serving from server cache");
//                    return ReportExportHelper.ExportFromCache(
//                        reportKey, upperFormat,
//                        "MemberDetailReport",
//                        _jsReportService, _logger);
//                }

//                // -- VIEW PATH ---------------------------------------------
//                _logger.LogInformation("?? DB CALL — fetching fresh data");

//                var allMemberData = await _memberDetail.GetMemberRegistrationDetail(request);

//                if (allMemberData == null || !allMemberData.Any())
//                    return NotFound(new { success = false, message = "No data found" });

//                var headerData = await _memberDetail.GetCommonHeaders();

//                // ? WebRootPath resolved from appsettings.json
//                var webRoot = ReportUtils.GetWebRootPath(
//                    _webHostEnvironment, _reportSettings, _logger);

//                //ReportUtils.ConvertLogoToBase64(headerData, webRoot, _logger);
//                await ReportUtils.ConvertUniqueImagesToBase64Async(headerData, nameof(CommonHeader.CompanyLogo), webRoot, _logger);

//                var reportData = new Dictionary<string, object>
//                {
//                    { "StudentDataSet", allMemberData },
//                    { "HeaderDataSet",  headerData },
//                    { "TotalRecords",   allMemberData.Count() }
//                };

//                var htmlContent = await _jsReportService.RenderAndCacheReport(
//                    reportKey: reportKey,
//                    reportPath: "Views/Report/MemberReport.cshtml",
//                    data: reportData
//                );

//                if (upperFormat == "VIEW")
//                {
//                    var pdfBytes =await _jsReportService.GenerateReportFromHtml(htmlContent, "PDF");
//                    return Ok(new
//                    {
//                        success = true,
//                        pdfData = Convert.ToBase64String(pdfBytes),
//                        reportName = "Member Detail Report"
//                    });
//                }

//                return ReportExportHelper.ExportFromCache(
//                    reportKey, upperFormat,
//                    "MemberDetailReport",
//                    _jsReportService, _logger);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error in generate-report");
//                return StatusCode(500, new { success = false, error = ex.Message });
//            }
//        }
//    }
//}






using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface;
using NexgenCosysReport.Utils.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace NexgenCosysReport.Controllers.Member
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

        [HttpPost("MemberDetailReport")]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] MemberDetailRequest request,
            [FromQuery] string format = "VIEW")
        {
           

            try
            {
                var response = new GeneralResponse<ReportResponseDtos>();   
                if (request == null || !ModelState.IsValid)
                {
                    response.IsValid = false;
                    response.StatusCode = 400;
                    response.Message = "Invalid request";
                    return BadRequest(response);
                }
                    //return BadRequest(new { success = false, message = "Invalid request" });

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberReport");

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.IsHtmlCached(reportKey), _logger);

                // -- EXPORT PATH -------------------------------------------
                if (upperFormat != "VIEW" && _jsReportService.IsHtmlCached(reportKey))
                {
                    _logger.LogInformation("? NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        "MemberDetailReport",
                        _jsReportService, _logger);
                }

                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings, _logger);

                // --------------------------------------------------------
                // STAGE 1 — DB queries concurrently
                // --------------------------------------------------------
              

                var memberTask = _memberDetail.GetMemberRegistrationDetail(request);
                var headerTask = _memberDetail.GetCommonHeaders();

                await Task.WhenAll(memberTask, headerTask);

                var allMemberData = memberTask.Result;
                var headerData = headerTask.Result;

              

                if (!allMemberData.Any())
                    return NotFound(new { success = false, message = "No data found" });

                // --------------------------------------------------------
                // STAGE 2 — CompanyLogo is common — read ONCE
                // --------------------------------------------------------
              

                var header = headerData.FirstOrDefault();
                var companyLogoPath = header?.CompanyLogo ?? "";

                var companyLogoBase64 = await ReportUtils.ReadCommonImageAsBase64Async(
                                            webRoot, companyLogoPath, _logger);

                if (header != null)
                    header.CompanyLogo = companyLogoBase64;

               

                // --------------------------------------------------------
                // STAGE 3 — Razor render
                // --------------------------------------------------------
              

                var reportData = new Dictionary<string, object>
                {
                    { "StudentDataSet", allMemberData       },
                    { "HeaderDataSet",  headerData          },
                    { "TotalRecords",   allMemberData.Count }
                };

                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/MemberReport.cshtml",
                    data: reportData);

            

                // --------------------------------------------------------
                // STAGE 4 — PDF generation
                // --------------------------------------------------------
            

                var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                    htmlContent, "PDF");

           

                if (upperFormat == "VIEW")
                {
                    //return Ok(new
                    //{
                    //    success = true,
                    //    pdfData = Convert.ToBase64String(pdfBytes),
                    //    reportName = "Member Detail Report"
                    //});

                    response.IsValid = true;
                    response.StatusCode = 200;
                    response.Message = "Success";
                    response.Data = new ReportResponseDtos
                    {
                        PdfData = Convert.ToBase64String(pdfBytes),
                        ReportName = "MemberIdCard Report",
                        Pagination = new Pagination
                        {
                            CurrentPage = request.currentPage,
                            TotalPages = 1,
                            TotalRecord = 1,
                            PageSize = request.pageSize,
                            HasNextPage = false,
                            HasPreviousPage = false
                        }
                    };
                    return Ok(response);
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    "MemberDetailReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
