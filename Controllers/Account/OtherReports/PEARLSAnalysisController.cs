// Controllers/AccountOperation/OthersReport/PEARLSAnalysisController.cs
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
using System.Text.Json;

namespace NexgenCosysReport.Controllers.AccountOperation.OthersReport
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class PEARLSAnalysisController : ControllerBase
    {
        private readonly IPEARLSAnalysis _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<PEARLSAnalysisController> _logger;
        private readonly IDateConverterService _dateConverter;

        public PEARLSAnalysisController(
            IPEARLSAnalysis repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<PEARLSAnalysisController> logger,
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

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] PEARLSAnalysisRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "PEARLSAnalysis";
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
                var data = await _repository.GetPEARLSAnalysisDataAsync(request);

                if (!data.PEARLSP.Any() && !data.PEARLSE.Any() && !data.PEARLSA.Any() &&
                    !data.PEARLSR.Any() && !data.PEARLSL.Any() && !data.PEARLSS.Any())
                {
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });
                }

                // Get header data
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    !request.BranchId.Contains(',') &&
                    long.TryParse(request.BranchId, out _))
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
                    { "PEARLSP", data.PEARLSP },
                    { "PEARLSE", data.PEARLSE },
                    { "PEARLSA", data.PEARLSA },
                    { "PEARLSR", data.PEARLSR },
                    { "PEARLSL", data.PEARLSL },
                    { "PEARLSS", data.PEARLSS },
                    { "TillDate", data.TillDate },
                    { "BranchNames", data.BranchNames ?? "All Branches" },
                    { "FiscalYear", data.FiscalYear },
                    { "PreviousDate", data.PreviousDate },
                    { "HeaderDataSet", headerData },
                    { "Format", upperFormat },
                    { "VisualReport", request.VisualReport }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VPEARLSAnalysisReport.cshtml"
                    : "Views/Report/Account/OtherReports/PEARLSAnalysisReport.cshtml";

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
                        totalRecord = data.PEARLSP.Count + data.PEARLSE.Count + data.PEARLSA.Count +
                                      data.PEARLSR.Count + data.PEARLSL.Count + data.PEARLSS.Count
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
                _logger.LogError(ex, "Error generating PEARLS Analysis Report");
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