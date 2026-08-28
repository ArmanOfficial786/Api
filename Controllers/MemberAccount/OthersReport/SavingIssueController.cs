// Controllers/MemberAccount/OthersReport/SavingIssueController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MemberAccount.OthersReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SavingIssueController : ControllerBase
    {
        private readonly ISavingIssueRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingIssueController> _logger;
        private readonly IDateConverterService _dateConverter;

        public SavingIssueController(
            ISavingIssueRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<SavingIssueController> logger,
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

        [HttpPost("GenerateReport")]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] SavingIssueRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                // Extract userId from JWT
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                if (request.ReportMode == "DateWise")
                {
                    if (string.IsNullOrEmpty(request.FromDateBs) || request.FromDateBs == "-1")
                        return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "From date is required for Date Wise report" });
                    if (string.IsNullOrEmpty(request.ToDateBs) || request.ToDateBs == "-1")
                        return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "To date is required for Date Wise report" });

                    var fromDate = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                    var toDate = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                    if (fromDate > toDate)
                    {
                        return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "From date cannot be greater than To date" });
                    }
                }

                if (request.ReportMode == "DepositTypeWise" && (!request.DepositTypeId.HasValue || request.DepositTypeId.Value == -1))
                    return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "Please select a deposit type for Deposit Type Wise report" });

                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                    return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "Please select at least one branch" });

                var reportName = "SavingIssue";
                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

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

                var data = await _repository.GetReportDataAsync(request);

                if (!data.Rows.Any())
                {
                    return NotFound(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 404,
                        message = "No saving accounts found for the selected criteria"
                    });
                }

                // Header data
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

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalOpeningBalance", data.TotalOpeningBalance },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDateBs },
                    { "ToDate", request.ToDateBs },
                    { "BranchNames", request.BranchName },
                    { "OrderBy", request.OrderBy },
                    { "ReportMode", request.ReportMode },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport },
                    { "DepositTypeName", data.DepositTypeName },
                    { "CollectorName", data.CollectorName },
                    { "MemberGroupName", data.MemberGroupName },
                    { "TotalAccounts", data.TotalAccounts }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VSavingIssueReport.cshtml"
                    : "Views/Report/MemberAC/SavingIssueReport.cshtml";

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
                _logger.LogError(ex, "Saving Issue report generation failed");
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