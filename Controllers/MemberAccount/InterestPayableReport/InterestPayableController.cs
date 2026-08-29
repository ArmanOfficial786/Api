// Controllers/MemberAccount/InterestPayableReport/InterestPayableController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Interfaces.ServiceInterface.MemberAccount.InterestPayableReport;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MemberAccount.InterestPayableReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InterestPayableController : ControllerBase
    {
        private readonly IInterestPayableRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<InterestPayableController> _logger;
        private readonly IDateConverterService _dateConverter;

        public InterestPayableController(
            IInterestPayableRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<InterestPayableController> logger,
            IDateConverterService dateConverter)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _dateConverter = dateConverter;
        }

        [HttpPost()]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] InterestPayableRequestDto request,
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
                if (string.IsNullOrEmpty(request.TillDateBs) || request.TillDateBs == "-1")
                    return BadRequest(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 400,
                        message = "Till date is required"
                    });

                // Validate office selection
                if (string.IsNullOrEmpty(request.OfficeId) || request.OfficeId == "-1")
                    return BadRequest(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 400,
                        message = "Please select an office"
                    });

                var reportName = $"InterestPayable_{request.ReportView}";
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

                // Get report data
                var data = await _repository.GetReportDataAsync(request);

                if (!data.Rows.Any())
                {
                    return NotFound(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 404,
                        message = "No interest payable records found for the selected criteria"
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
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalInterest", data.TotalInterest },
                    { "TotalTax", data.TotalTax },
                    { "TotalBalance", data.TotalBalance },
                    { "HeaderDataSet", headerData },
                    { "TillDate", request.TillDateBs },
                    { "OfficeName", request.OfficeName },
                    { "OrderBy", request.OrderBy },
                    { "ReportView", request.ReportView },
                    { "ReportViewName", data.ReportViewName ?? "All" },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport },
                    { "TotalDepositTypes", data.TotalDepositTypes }
                };

                // Render view
                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VInterestPayableReport.cshtml"
                    : "Views/Report/MemberAC/InterestPayableReport.cshtml";

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
                        totalRecord = data.Rows.Count
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
                                totalRecord = data.Rows.Count
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
                _logger.LogError(ex, "Interest Payable report generation failed");
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