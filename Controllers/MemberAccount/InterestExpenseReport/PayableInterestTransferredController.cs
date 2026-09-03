// Controllers/MemberAccount/InterestExpenseReport/PayableInterestTransferredController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestExpenseReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.InterestExpenseReportInterface;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MemberAccount.InterestExpenseReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PayableInterestTransferredController : ControllerBase
    {
        private readonly IPayableInterestTransferredRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<PayableInterestTransferredController> _logger;
        private readonly IDateConverterService _dateConverter;

        public PayableInterestTransferredController(
            IPayableInterestTransferredRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<PayableInterestTransferredController> logger,
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
        public async Task<IActionResult> GenerateReport(
            [FromBody] PayableInterestTransferredRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                // Extract userId from JWT
                var userIdClaim = User.FindFirst("UserId")?.Value
                                  ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                }

                var reportName = "PayableInterestTransferred";
                var upperFormat = format.ToUpper();
                if (request == null || !ModelState.IsValid)
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "Invalid request" });
                }
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

                // Get report data
                var data = await _repository.GetReportDataAsync(request);

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }


                var headerData = await _commonHeaderRepository.GetCommonHeaders();

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
                    { "TotalNetBalance", data.TotalNetBalance },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDateBs },
                    { "ToDate", request.ToDateBs },
                    { "BranchNames", request.BranchName },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport },
                    { "TotalDepositTypes", data.TotalDepositTypes }
                };

                // Render view
                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VPayableInterestTransferredReport.cshtml"
                    : "Views/Report/MemberAC/InterestExpenseReport/PayableInterestTransferredReport.cshtml";

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