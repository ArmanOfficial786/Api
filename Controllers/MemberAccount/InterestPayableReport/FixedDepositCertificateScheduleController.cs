// Controllers/MemberAccount/FixedDepositCertificateSchedule/FixedDepositCertificateScheduleController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.FixedDepositCertificateSchedule;
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

        [HttpPost("GenerateReport")]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] FixedDepositCertificateScheduleRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                // Extract userId from JWT
                var userIdClaim = User.FindFirst("UserId")?.Value
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 401,
                        message = "User not authenticated"
                    });
                }

                // Validate request
                if (request.AccountId == -1)
                    return BadRequest(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 400,
                        message = "Please select an account"
                    });

                var reportName = $"FixedDeposit{request.ReportType}";
                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                // Check cache
                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    var cachedResult = await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);

                    if (cachedResult is FileContentResult fileResult)
                    {
                        return Ok(new GeneralResponse<ReportResponseDtos>
                        {
                            isValid = true,
                            statusCode = 200,
                            message = "Report generated from cache",
                            data = new ReportResponseDtos
                            {
                                pdfData = Convert.ToBase64String(fileResult.FileContents),
                                reportName = $"{reportName}.{upperFormat.ToLower()}"
                            }
                        });
                    }
                }

                // Get report data based on report type
                FixedDepositCertificateScheduleData data;
                if (request.ReportType == "Schedule")
                {
                    data = await _repository.GetScheduleDataAsync(request);
                }
                else
                {
                    data = await _repository.GetCertificateDataAsync(request);
                }

                if (data.CertificateDetail == null)
                {
                    return NotFound(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 404,
                        message = "No fixed deposit account found for the selected criteria"
                    });
                }

                // Get header data
                var officeIdClaim = User.FindFirst("OfficeId")?.Value;
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(officeIdClaim) && long.TryParse(officeIdClaim, out var officeId))
                {
                    branchIdForHeader = officeId.ToString();
                }

                var headerData = await _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

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

                var exportResult = await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);

                if (exportResult is FileContentResult fileResult2)
                {
                    return Ok(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = true,
                        statusCode = 200,
                        message = "Report generated successfully",
                        data = new ReportResponseDtos
                        {
                            pdfData = Convert.ToBase64String(fileResult2.FileContents),
                            reportName = $"{reportName}.{upperFormat.ToLower()}",
                            pagination = new Pagination
                            {
                                currentPage = 1,
                                totalPages = 1,
                                pageSize = 1,
                                totalRecord = request.ReportType == "Schedule" ? data.TotalRecords : 1
                            }
                        }
                    });
                }

                return Ok(new GeneralResponse<ReportResponseDtos>
                {
                    isValid = true,
                    statusCode = 200,
                    message = "Report generated successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new GeneralResponse<ReportResponseDtos>
                {
                    isValid = false,
                    statusCode = 400,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fixed Deposit Certificate/Schedule report generation failed");
                return StatusCode(500, new GeneralResponse<ReportResponseDtos>
                {
                    isValid = false,
                    statusCode = 500,
                    message = "An error occurred while generating the report"
                });
            }
        }
    }
}