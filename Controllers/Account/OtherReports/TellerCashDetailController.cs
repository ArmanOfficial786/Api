// Controllers/AccountOperation/OthersReport/TellerCashDetailController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.AccountOperation.OthersReport;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.AccountOperation.OthersReport
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class TellerCashDetailController : ControllerBase
    {
        private readonly ITellerCashDetailRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<TellerCashDetailController> _logger;
        private readonly IDateConverterService _dateConverter;

        public TellerCashDetailController(
            ITellerCashDetailRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<TellerCashDetailController> logger,
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

        // POST api/TellerCashDetail?format=VIEW
        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] TellerCashDetailRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                //var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                //{
                //    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                //}

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }

                var reportName = "TellerCashDetail";
                var upperFormat = format.ToUpper();

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

                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    request.BranchId != "string" &&
                    long.TryParse(request.BranchId, out _))
                {
                    branchIdForHeader = request.BranchId;
                }

                var dataTask = _repository.GetReportDataAsync(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.TransactionRows.Any() && !data.ManualRows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "TransactionRows", data.TransactionRows },
                    { "ManualRows", data.ManualRows },
                    { "TransactionCashBalanceDR", data.TransactionCashBalanceDR },
                    { "TransactionCashBalanceCR", data.TransactionCashBalanceCR },
                    { "VoucherCashBalanceDR", data.VoucherCashBalanceDR },
                    { "VoucherCashBalanceCR", data.VoucherCashBalanceCR },
                    { "TellerCashFromVault", data.TellerCashFromVault },
                    { "TellerCashFromTeller", data.TellerCashFromTeller },
                    { "TellerCashToTeller", data.TellerCashToTeller },
                    { "TellerCashToVault", data.TellerCashToVault },
                    { "TotalDR", data.TotalDR },
                    { "TotalCR", data.TotalCR },
                    { "TotalBalance", data.TotalBalance },
                    { "GrandTotal", data.GrandTotal },
                    { "HeaderDataSet", headerData },
                    { "TransactionDate", data.TransactionDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "TellerName", data.TellerName ?? "All Teller" },
                    { "OrderBy", data.OrderBy ?? "" },
                    { "ReportType", data.ReportType },
                    { "Format", upperFormat }
                };

                string viewPath = "Views/Report/AccountOperation/OthersReport/TellerCashDetailReport.cshtml";

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
                        totalRecord = data.TransactionRows.Count + data.ManualRows.Count
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
                _logger.LogError(ex, "Error generating TellerCashDetail report");
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