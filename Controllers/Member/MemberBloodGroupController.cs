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
    [ApiController]
    [Route("api/[controller]")]
    public class MemberBloodGroupReportController : ControllerBase
    {
        private readonly IMemberBloodGroup _bloodGroupReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberBloodGroupReportController> _logger;

        public MemberBloodGroupReportController(
            IMemberBloodGroup bloodGroupReportService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberBloodGroupReportController> logger,
            CustomHeaderResponse headerResponse)
        {
            _bloodGroupReportService = bloodGroupReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }

        [HttpPost]
        public async Task<ActionResult> GenerateReport(
            [FromBody] MemberBloodGroupReportRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "MemberBloodGroupReport";
                var upperFormat = format.ToUpper();
                var response = new GeneralResponse<ReportResponseDtos>();

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request. MemberRegistration is required." });

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);
                }

                // Parallel calls
                var dataTask = _bloodGroupReportService.GetMemberBloodGroupReport(request);
                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchSelected) &&
                    request.BranchSelected != "-1" && !request.BranchSelected.Contains(','))
                {
                    branchIdForHeader = request.BranchSelected;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Any())
                {
                    response.isValid = false;
                    response.statusCode = 404;
                    response.message = "No data found";
                    return NotFound(response);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "BloodGroupDataSet", data },
                    { "HeaderDataSet", headerData },
                    { "TotalRecords", data.Count },
                    { "BranchName", request.BranchName ?? "All" },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "Format", upperFormat },
                    { "BloodGroupOption", request.BloodGroupOption },
                    { "SameCompanyName", request.SameCompanyName }
                };

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/VisualReport/VMemberBloodGroupReport.cshtml"
                            : "Views/Report/MemberBloodGroupReport.cshtml",
                        data: reportData));

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);

                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        totalRecord = 0,
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false
                    };

                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
                    Response.Headers.Append("Content-Disposition", "inline; filename=\"MemberDetailSummary.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }


                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemberBloodGroupReport generation failed");
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