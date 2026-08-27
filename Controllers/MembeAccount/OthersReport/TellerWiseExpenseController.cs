// Controllers/AccountOperation/TellerWiseExpenseController.cs
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

namespace NexgenCosysReport.Controllers.MembeAccount.OthersReport
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]
    public class TellerWiseExpenseController : ControllerBase
    {
        private readonly ITellerExpense _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<TellerWiseExpenseController> _logger;
        private readonly IDateConverterService _dateConverter;

        public TellerWiseExpenseController(
            ITellerExpense repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<TellerWiseExpenseController> logger,
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

        /// <summary>
        /// Generate Teller Wise Expense Report (replaces btnViewReport_Click)
        /// POST /api/TellerWiseExpense/GenerateReport?format=VIEW
        /// </summary>
        [HttpPost()]
        public async Task<ActionResult<GeneralResponse<ReportResponseDtos>>> GenerateReport(
            [FromBody] TellerWiseExpenseRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var userId = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 401, message = "User not authenticated" });

                // Validate request
                if (string.IsNullOrEmpty(request.FromDateBs) || request.FromDateBs == "-1")
                    return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "From date is required" });
                if (string.IsNullOrEmpty(request.ToDateBs) || request.ToDateBs == "-1")
                    return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "To date is required" });

                var fromDate = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDate = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                if (fromDate > toDate)
                {
                    return BadRequest(new GeneralResponse<ReportResponseDtos> { isValid = false, statusCode = 400, message = "From date cannot be greater than To date" });
                }

                var reportName = "TellerWiseExpense";
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

                // Get report data
                var data = await _repository.GetReportDataAsync(request);

                if (!data.Rows.Any())
                {
                    return NotFound(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 404,
                        message = "No transactions found for the selected period and teller"
                    });
                }

                // Header data
                var officeIdClaim = User.FindFirst("OfficeId")?.Value;
                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(officeIdClaim) && long.TryParse(officeIdClaim, out var officeId))
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
                    { "TotalSavingWithdrawlAmount", data.TotalSavingWithdrawlAmount },
                    { "TotalShareReturnAmount", data.TotalShareReturnAmount },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "TotalMiscellaneousAmount", data.TotalMiscellaneousAmount },
                    { "TotalAmount", data.TotalAmount },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDateBs },
                    { "ToDate", request.ToDateBs },
                    { "TellerName", data.TellerName ?? "All Tellers" },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VTellerWiseExpenseReport.cshtml"
                    : "Views/Report/MemberAC/OthersReport/TellerWiseExpense.cshtml";

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
                _logger.LogError(ex, "TellerWiseExpense report generation failed");
                return StatusCode(500, new GeneralResponse<ReportResponseDtos>
                {
                    isValid = false,
                    statusCode = 500,
                    message = ex.Message
                });
            }
        }

    }
}