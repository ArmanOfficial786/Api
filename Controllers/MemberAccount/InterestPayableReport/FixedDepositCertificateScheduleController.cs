using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MemberAccount.FixedDepositCertificateSchedule
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FixedDepositCertificateScheduleController : ControllerBase
    {
        private readonly IFixedDepositCertificateScheduleRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<FixedDepositCertificateScheduleController> _logger;

        // Both Certificate and Schedule report types render through one shared Razor view.
        private const string ViewPath = "Views/Report/MemberAC/InterestPayableReport/FixedDepositCertificateScheduleReport.cshtml";

        public FixedDepositCertificateScheduleController(
            IFixedDepositCertificateScheduleRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<FixedDepositCertificateScheduleController> logger)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] FixedDepositCertificateScheduleRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                var userIdClaim = User.FindFirst("UserId")?.Value
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                }

                var reportName = $"FixedDeposit{request.ReportType}";
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

                FixedDepositCertificateScheduleData data;

                var officeIdClaim = User.FindFirst("OfficeId")?.Value;
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(officeIdClaim) && long.TryParse(officeIdClaim, out var officeId))
                {
                    branchIdForHeader = officeId.ToString();
                }

                Task<FixedDepositCertificateScheduleData> dataTask = request.ReportType == "Schedule"
                    ? _repository.GetScheduleDataAsync(request, userId)
                    : _repository.GetCertificateDataAsync(request, userId);

                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                data = await dataTask;
                var headerData = await headerTask;

                if (data.CertificateDetail == null)
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "CertificateDetail", data.CertificateDetail },
                    { "ScheduleRows", data.ScheduleRows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalInterest", data.TotalInterest },
                    { "TotalTax", data.TotalTax },
                    { "TotalNetAmount", data.TotalNetAmount },
                    { "HeaderDataSet", headerData },
                    { "AccountNo", data.AccountNo ?? "" },
                    { "MemberId", data.MemberId ?? "" },
                    { "MemberName", data.MemberName ?? "" },
                    { "ShowHeader", request.ShowHeader },
                    { "ReportType", request.ReportType },
                    { "Format", upperFormat }
                };

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: ViewPath,
                        data: reportData));

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false,
                        totalRecord = request.ReportType == "Schedule" ? data.TotalRecords : 1
                    };

                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                   reportKey, upperFormat,
                   reportName,
                   _jsReportService, _logger);
            }
            catch (Exception ex)
            {
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