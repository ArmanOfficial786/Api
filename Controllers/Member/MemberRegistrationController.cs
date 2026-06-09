//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Dtos.RequestDtos.Member;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Member;
//using NexgenCosysReport.Utils.Enum;
//using NexgenCosysReport.Utils.Report;


//namespace NexgenCosysReport.Controllers.Member
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class MemberRegistrationController : ControllerBase
//    {
//        private readonly IMemberDetail _memberDetail;
//        private readonly IJsReportService _jsReportService;
//        private readonly ICommonHeaderRepository _commonHeaderRepository;
//        private readonly IReportFileResponse _reportFileResponse;
//        private readonly ILogger<MemberRegistrationController> _logger;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly IOptions<ReportSettings> _reportSettings;

//        private static readonly PageSizeSetting PageSetting =
//           PageSizeSetting.Custom(420, 297, PageUnit.mm, landscape: true);

//        public MemberRegistrationController(
//            IMemberDetail memberDetail,
//            IJsReportService jsReportService,
//            ILogger<MemberRegistrationController> logger,
//            IWebHostEnvironment webHostEnvironment,
//            IOptions<ReportSettings> reportSettings,
//            IReportFileResponse reportFileResponse,
//            ICommonHeaderRepository commonHeaderRepository)
//        {
//            _memberDetail = memberDetail;
//            _jsReportService = jsReportService;
//            _logger = logger;
//            _webHostEnvironment = webHostEnvironment;
//            _reportSettings = reportSettings;
//            _reportFileResponse = reportFileResponse;
//            _commonHeaderRepository = commonHeaderRepository;
//        }

//        [HttpPost]
//        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
//            [FromBody] MemberDetailRequest request,
//            [FromQuery] string format = "VIEW",
//            CancellationToken ct = default)
//        {


//            try
//            {
//                var response = new GeneralResponse<ReportResponseDtos>();
//                if (request == null || !ModelState.IsValid)
//                {
//                    response.isValid = false;
//                    response.statusCode = StatusCodes.Status400BadRequest;
//                    response.message = "Invalid request";
//                    return BadRequest(response);
//                }
//                //return BadRequest(new { success = false, message = "Invalid request" });

//                var upperFormat = format.ToUpper();
//                var reportKey = ReportUtils.GenerateReportKey(request, "MemberReport") + $"_{upperFormat}";

//                ReportExportHelper.LogCacheState(
//                    upperFormat, reportKey,
//                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

//                // -- EXPORT PATH -------------------------------------------
//                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
//                {
//                    _logger.LogInformation("? NO DB CALL — serving from cache");
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        reportKey, upperFormat,
//                        "MemberDetailReport",
//                        _jsReportService, _logger, PageSetting, ct);
//                }

//                var webRoot = ReportUtils.GetWebRootPath(
//                    _webHostEnvironment, _reportSettings);

//                // --------------------------------------------------------
//                // STAGE 1 — DB queries concurrently
//                // --------------------------------------------------------


//                var memberTask = _memberDetail.GetMemberRegistrationDetail(request);
//                var headerTask = _commonHeaderRepository.GetCommonHeaders();

//                await Task.WhenAll(memberTask, headerTask);

//                var allMemberData = memberTask.Result;
//                var headerData = headerTask.Result;



//                if (!allMemberData.Any())
//                    return NotFound(new { success = false, message = "No data found" });

//                // --------------------------------------------------------
//                // STAGE 2 — CompanyLogo is common — read ONCE
//                // --------------------------------------------------------


//                var header = headerData.FirstOrDefault();
//                var companyLogoPath = header?.CompanyLogo ?? "";

//                var companyLogoBase64 = await ReportUtils.ReadCommonImageAsBase64Async(
//                                            webRoot, companyLogoPath, _logger);

//                if (header != null)
//                    header.CompanyLogo = companyLogoBase64;



//                // --------------------------------------------------------
//                // STAGE 3 — Razor render
//                // --------------------------------------------------------


//                var reportData = new Dictionary<string, object>
//                {
//                    { "StudentDataSet", allMemberData       },
//                    { "HeaderDataSet",  headerData          },
//                    { "TotalRecords",   allMemberData.Count },
//                    { "Format",         upperFormat         },
//                    { "FromDate",       request.fromDate    },
//                    { "ToDate",         request.toDate      },
//                    //{ "BranchName", branchName },//needed
//                };

//                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                    reportKey: reportKey,
//                    reportPath: "Views/Report/MemberRegistrationReport.cshtml",
//                    data: reportData);



//                // --------------------------------------------------------
//                // STAGE 4 — PDF generation
//                // --------------------------------------------------------


//                if (upperFormat == "VIEW")
//                {

//                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey, PageSetting, ct);
//                    return _reportFileResponse.BuildPdfResponse(pdfBytes);
//                }

//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat,
//                    "MemberDetailReport",
//                    _jsReportService, _logger, PageSetting, ct);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { success = false, error = ex.Message });
//            }
//        }
//    }
//}






using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.Member
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberRegistrationController : ControllerBase
    {
        private readonly IMemberDetail _memberDetail;
        private readonly IJsReportService _jsReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly ILogger<MemberRegistrationController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;

        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(594, 420, PageUnit.mm, landscape: true);

        public MemberRegistrationController(
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            ILogger<MemberRegistrationController> logger,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            IReportFileResponse reportFileResponse,
            ICommonHeaderRepository commonHeaderRepository)
        {
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _reportFileResponse = reportFileResponse;
            _commonHeaderRepository = commonHeaderRepository;
        }

        [HttpPost]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] MemberDetailRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var response = new GeneralResponse<ReportResponseDtos>();
                if (request == null || !ModelState.IsValid)
                {
                    response.isValid = false;
                    response.statusCode = StatusCodes.Status400BadRequest;
                    response.message = "Invalid request";
                    return BadRequest(response);
                }

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberRegistrationReport") + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("? NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, "MemberDetailReport",
                        _jsReportService, _logger, PageSetting, ct);
                }
                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                var memberTask = _memberDetail.GetMemberRegistrationDetail(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders();
                await Task.WhenAll(memberTask, headerTask);

                var allMemberData = memberTask.Result;
                var headerData = headerTask.Result;

                if (!allMemberData.Any())
                    return NotFound(new { success = false, message = "No data found" });


                var header = headerData.FirstOrDefault();
                var companyLogoPath = header?.CompanyLogo ?? "";
                var companyLogoBase64 = await ReportUtils.ReadCommonImageAsBase64Async(
                                            webRoot, companyLogoPath, _logger);
                if (header != null)
                    header.CompanyLogo = companyLogoBase64;


                var reportData = new Dictionary<string, object>
                {
                    { "StudentDataSet", allMemberData       },
                    { "HeaderDataSet",  headerData          },
                    { "TotalRecords",   allMemberData.Count },
                    { "Format",         upperFormat         },
                    { "FromDate",       request.fromDate    },
                    { "ToDate",         request.toDate      },
                    { "BranchName",     "All"               },
                };

                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: request.VisualReport ? "Views/VisualReport/VMemberRegistrationReport.cshtml" : "Views/Report/MemberRegistrationReport.cshtml",
                    data: reportData);

                if (upperFormat == "VIEW")
                {
                    return Content(htmlContent, "text/html");
                }

                if (upperFormat == "PDF")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        htmlContent, "PDF", reportKey, PageSetting, ct);
                    return _reportFileResponse.BuildPdfResponse(pdfBytes);
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, "MemberDetailReport",
                    _jsReportService, _logger, PageSetting, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemberRegistration report failed");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}