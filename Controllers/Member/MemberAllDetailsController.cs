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
    [Route("api/[controller]")]
    [ApiController]
    public class MemberAllDetailsController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IMemberAllDetails _memberAllDetails;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberAllDetailsController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private static readonly PageSizeSetting PageSetting =
         PageSizeSetting.Custom(594, 420, PageUnit.mm, landscape: true);

        public MemberAllDetailsController(IJsReportService jsReportService, ICommonHeaderRepository commonHeaderRepository, IReportFileResponse reportFileResponse, IOptions<ReportSettings> reportSettings, ILogger<MemberAllDetailsController> logger, IWebHostEnvironment webHostEnvironment, IMemberAllDetails memberAllDetails)
        {
            _jsReportService = jsReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _reportFileResponse = reportFileResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _memberAllDetails = memberAllDetails;
        }

        [HttpPost]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GetMemberAllDetails(
            [FromBody] MemberAllDetailRequst request,
            [FromQuery] string format = "View",
            CancellationToken ct = default
            )
        {
            var response = new GeneralResponse<ReportResponseDtos>();

            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    response.isValid = false;
                    response.statusCode = StatusCodes.Status400BadRequest;
                    response.message = "Invalid request";
                    return BadRequest(response);
                }

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberAllDetails") + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, "MemberDetailReport",
                        _jsReportService, _logger, PageSetting, ct);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                var headerTask = _commonHeaderRepository.GetCommonHeaders();
                var memberTask = _memberAllDetails.GetMemberAllDetailsAsync(request);
                await Task.WhenAll(memberTask, headerTask);

                var allMemberData = memberTask.Result;
                var headerData = headerTask.Result;

                if (!allMemberData.Any())
                {
                    response.isValid = false;
                    response.statusCode = StatusCodes.Status404NotFound;
                    response.message = "No data found";
                    return NotFound(response);
                }


                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));

                await ReportUtils.AttachMemberPhotosAsync(
                    allMemberData,
                    idPropertyName: nameof(MemberAllDetailSpResponse.MemberId),
                    imagePropertyName: nameof(MemberAllDetailSpResponse.MemberImage),
                    webRoot: webRoot,
                    logger: _logger);


                var reportData = new Dictionary<string, object>
                {
                    { "MemberAllDetailDataSet", allMemberData       },
                    { "HeaderDataSet",          headerData          },
                    { "Format",                 upperFormat         },
                    { "FromDate",               request.fromDate    },
                    { "ToDate",                 request.toDate      },
                    { "BranchName",             "All"               },
                    { "SelectedColumns",        request.SelectedColumns ?? new List<string>() },
                };

                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                   reportKey: reportKey,
                   //reportPath: "Views/Report/MemberAllDetails.cshtml",
                   reportPath: request.visualReport ? "Views/VisualReport/Member/VMemeberAllDetailReport.cshtml" : "Views/Report/Member/MemberAllDetails.cshtml",
                   data: reportData);

                if (upperFormat == "VIEW")
                {
                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
                     htmlContent, reportKey, ct);

                    _logger.LogInformation("?? VIEW — jsreport Html recipe, {Bytes:N0} chars", viewHtml.Length);
                    return Content(viewHtml, "text/html");

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
                _logger.LogError(ex, "Error generating MemberAllDetails report");

                response.isValid = false;
                response.statusCode = StatusCodes.Status500InternalServerError;
                response.message = "An error occurred while generating the report";
                return StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }
    }
}