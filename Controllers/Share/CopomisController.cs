using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Share;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Share;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Share
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CopomisController : ControllerBase
    {
        private readonly ICopomis _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly ILogger<CopomisController> _logger;
        private readonly IDateConverterService _dateConverter;

        private static readonly PageSizeSetting PageSetting =
      PageSizeSetting.Custom(594, 420, PageUnit.mm, landscape: true);

        public CopomisController(
            ICopomis repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<CopomisController> logger,
            IDateConverterService dateConverter,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _dateConverter = dateConverter;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] CopomisRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default
            )
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                }

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }


                var reportName = "Copomis";
                var upperFormat = format.ToUpper();

                var reportKey = ReportUtils.GenerateReportKey(request, reportName);

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                var dataTask = _repository.GetReportDataAsync(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders();

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);



                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalMembers", data.TotalMembers },
                    { "TotalShareNo", data.TotalShareNo },
                    { "TotalShareAmount", data.TotalShareAmount },
                    { "HeaderDataSet", headerData },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "All" },
                    { "MemberTypeName", data.MemberTypeName ?? "All" },
                    { "ShowMemberPhoto", request.ShowMemberPhoto },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VCopomisReport.cshtml"
                    : "Views/Report/Share/CopomisReport.cshtml";

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: viewPath,
                        data: reportData));

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
                _logger.LogError(ex, "Error generating Copomis report");
                return StatusCode(500, new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}