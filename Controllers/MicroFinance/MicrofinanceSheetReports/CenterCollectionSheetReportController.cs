// Controllers/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReportController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Microfinance.MicrofinanceSheetReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CenterCollectionSheetReportController : ControllerBase
    {
        private readonly ICenterCollectionSheetReportRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<CenterCollectionSheetReportController> _logger;

        public CenterCollectionSheetReportController(
            ICenterCollectionSheetReportRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<CenterCollectionSheetReportController> logger)
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
            [FromBody] CenterCollectionSheetReportRequestDto request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
                {
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
                }

                if (request == null || !ModelState.IsValid)
                {
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
                }



                var reportName = "CenterCollectionSheetReport";
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

                var dataTask = _repository.GetReportDataAsync(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders("");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Members.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Members", data.Members },
                    { "Receivables", data.Receivables },
                    { "SavingSummary", data.SavingSummary },
                    { "LoanSummary", data.LoanSummary },
                    { "Evaluation", data.Evaluation ?? new CenterCollectionSheetEvaluationDto() },

                    { "TotalMembers", data.TotalMembers },
                    { "TotalSaving1", data.TotalSaving1 },
                    { "TotalSaving2", data.TotalSaving2 },
                    { "TotalSaving3", data.TotalSaving3 },
                    { "TotalSaving4", data.TotalSaving4 },
                    { "TotalSaving5", data.TotalSaving5 },
                    { "TotalSaving6", data.TotalSaving6 },
                    { "TotalShare", data.TotalShare },
                    { "TotalLoan1", data.TotalLoan1 },
                    { "TotalLoan2", data.TotalLoan2 },
                    { "TotalLoan3", data.TotalLoan3 },
                    { "TotalLoan4", data.TotalLoan4 },
                    { "TotalLoan5", data.TotalLoan5 },
                    { "TotalLoan6", data.TotalLoan6 },
                    { "TotalReceivable", data.TotalReceivable },

                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "TillDateAd", data.TillDateAd ?? "" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "CollectionCenterAddress", data.CollectionCenterAddress ?? "" },
                    { "NextMeetingDate", data.NextMeetingDate ?? "" },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Microfinance/VCenterCollectionSheetReport.cshtml"
                    : "Views/Report/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReport.cshtml";

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
                        totalRecord = data.Members.Count
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CenterCollectionSheetReport");
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