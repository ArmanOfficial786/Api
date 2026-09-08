// Controllers/Account/OthersReport/ReserveMasterController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Account.OthersReport
{
    [ApiController]
    [Route("api/account/[controller]")]
    [Authorize]
    public class ReserveMasterController : ControllerBase
    {
        private readonly IReserveMasterRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<ReserveMasterController> _logger;

        public ReserveMasterController(
            IReserveMasterRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<ReserveMasterController> logger)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] ReserveMasterRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {

                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                }


                var reportName = "ReserveMaster";
                var upperFormat = format.ToUpper();

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

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
                var data = await _repository.GetReserveMasterDataAsync(request);

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });
                }

                // Get header data
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" && !request.BranchId.Contains(','))
                {
                    branchIdForHeader = request.BranchId;
                }

                var headerData = await _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                // Prepare report data
                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalReserveAmount", data.TotalReserveAmount },
                    { "TotalIncome", data.TotalIncome },
                    { "TotalExpense", data.TotalExpense },
                    { "NetProfit", data.NetProfit },
                    { "GeneralReserve", data.GeneralReserve },
                    { "RemainingReserve", data.RemainingReserve },
                    { "ReservePercentage", data.ReservePercentage },
                    { "FromDate", data.FromDateBs },
                    { "ToDate", data.ToDateBs },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "OrderBy", data.OrderBy },
                    { "HeaderDataSet", headerData },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport }
                };

                // Default: VisualReport = false -> use standard report view.
                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VReserveMasterReport.cshtml"
                    : "Views/Report/Account/OtherReports/ReserveMasterReport.cshtml";

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
                _logger.LogError(ex, "Error generating Reserve Master Report");
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