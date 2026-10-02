using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Member
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberIdCardController : ControllerBase
    {
        private readonly IMemberIdCard _memberIdCardService;
        private readonly IMemberDetail _memberDetail;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly ILogger<MemberIdCardController> _logger;

        public MemberIdCardController(
            IMemberIdCard memberIdCardService,
            IMemberDetail memberDetail,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberIdCardController> logger,
            CustomHeaderResponse headerResponse,
            ICommonHeaderRepository commonHeaderRepository)
        {
            _memberIdCardService = memberIdCardService;
            _memberDetail = memberDetail;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
            _commonHeaderRepository = commonHeaderRepository;
        }

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
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberIdCard");

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                // -- EXPORT — cache hit ? skip DB entirely -------------------------
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("? NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        "MemberIdCardReport",
                        _jsReportService, _logger);
                }

                var webRoot = ReportUtils.GetWebRootPath(
                    _webHostEnvironment, _reportSettings);

                // -- STAGE 1: DB queries — concurrent ------------------------------
                var memberTask = _memberIdCardService.GetMemberIdCardData(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders();
                await Task.WhenAll(memberTask, headerTask);

                var memberIdCardData = memberTask.Result;
                var headerData = headerTask.Result;

                if (!memberIdCardData.Any())
                    return NotFound(new { success = false, message = "No data found" });

                // -- STAGE 2: Common images — read once, concurrent -----------------
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

                // -- STAGE 3: Per-member unique images -----------------------------
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    memberIdCardData,
                    nameof(MemberIdCardModel.MemberPhoto),
                    webRoot);

                // -- STAGE 4: Razor render + cache HTML ----------------------------
                var reportData = new Dictionary<string, object>
                {
                    { "MemberIdCardDataSet", memberIdCardData       },
                    { "HeaderDataSet",       headerData             },
                    { "TotalRecords",        memberIdCardData.Count },
                };

                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: "Views/Report/Member/MemberIdCard.cshtml",
                    data: reportData);

                // -- STAGE 5: PDF generation ---------------------------------------

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                 htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        totalRecord = memberIdCardData.Count(),
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false
                    };
                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

                    Response.Headers.Append(
                        "Content-Disposition",
                        "inline; filename=\"MemberIdCardReport.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                // -- EXPORT — return binary blob with attachment header -------------
                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat,
                    "MemberIdCardReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _headerResponse.SetResponseHeaders(false, 500, $"Failed to generate report: {ex.Message}");
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