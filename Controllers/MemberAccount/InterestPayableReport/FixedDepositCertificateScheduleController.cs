// Controllers/MemberAccount/FixedDepositCertificateSchedule/FixedDepositCertificateScheduleController.cs
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

        [HttpGet()]
        public async Task<ActionResult<GeneralResponse<List<FixedDepositAccountListDto>>>> GetAccounts()
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new GeneralResponse<List<FixedDepositAccountListDto>>
                    {
                        isValid = false,
                        statusCode = 401,
                        message = "User not authenticated"
                    });
                }

                var accounts = await _repository.GetFixedDepositAccountsAsync(userId);

                return Ok(new GeneralResponse<List<FixedDepositAccountListDto>>
                {
                    isValid = true,
                    statusCode = 200,
                    message = "Accounts retrieved successfully",
                    data = accounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get fixed deposit accounts");
                return StatusCode(500, new GeneralResponse<List<FixedDepositAccountListDto>>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "An error occurred while retrieving accounts"
                });
            }
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

                // Check cache
                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat,
                        reportName,
                        _jsReportService, _logger);
                }

                // Get report data based on report type and header data in parallel
                FixedDepositCertificateScheduleData data;

                var officeIdClaim = User.FindFirst("OfficeId")?.Value;
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(officeIdClaim) && long.TryParse(officeIdClaim, out var officeId))
                {
                    branchIdForHeader = officeId.ToString();
                }

                // Create tasks for parallel execution
                Task<FixedDepositCertificateScheduleData> dataTask;
                if (request.ReportType == "Schedule")
                {
                    dataTask = _repository.GetScheduleDataAsync(request);
                }
                else
                {
                    dataTask = _repository.GetCertificateDataAsync(request);
                }

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

                // Build report data
                var reportData = new Dictionary<string, object>
                {
                    { "CertificateDetail", data.CertificateDetail },
                    { "ScheduleRows", data.ScheduleRows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalPrincipal", data.TotalPrincipal },
                    { "TotalInterest", data.TotalInterest },
                    { "TotalAmount", data.TotalAmount },
                    { "HeaderDataSet", headerData },
                    { "AccountNo", data.AccountNo ?? "" },
                    { "MemberId", data.MemberId ?? "" },
                    { "MemberName", data.MemberName ?? "" },
                    { "ShowHeader", request.ShowHeader },
                    { "ReportType", request.ReportType },
                    { "Format", upperFormat }
                };

                // Render view based on report type
                string viewPath = request.ReportType == "Schedule"
                    ? "Views/Report/MemberAC/FixedDepositScheduleReport.cshtml"
                    : "Views/Report/MemberAC/FixedDepositCertificateReport.cshtml";

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: viewPath,
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