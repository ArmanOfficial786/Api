// Controllers/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetPrintController.cs
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
    public class CenterCollectionSheetPrintController : ControllerBase
    {
        private readonly ICenterCollectionSheetPrintRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<CenterCollectionSheetPrintController> _logger;

        public CenterCollectionSheetPrintController(
            ICenterCollectionSheetPrintRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<CenterCollectionSheetPrintController> logger)
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
            [FromBody] CenterCollectionSheetPrintRequestDto request,
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



                var reportName = "CenterCollectionSheetPrint";
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

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Header", data.Header ?? new CenterCollectionSheetHeaderDto() },
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },

                    { "TotalSavingRemBalance", data.TotalSavingRemBalance },
                    { "TotalSavingPayableDeposit", data.TotalSavingPayableDeposit },
                    { "TotalSavingPayableWithdrawal", data.TotalSavingPayableWithdrawal },
                    { "TotalSavingPayableInt", data.TotalSavingPayableInt },
                    { "TotalLoanRemPrinciple", data.TotalLoanRemPrinciple },
                    { "TotalLoanPayablePri", data.TotalLoanPayablePri },
                    { "TotalLoanPayableInt", data.TotalLoanPayableInt },

                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "TillDateAd", data.TillDateAd ?? "" },
                    { "SheetType", data.SheetType ?? "A" },
                    { "SheetTypeName", data.SheetTypeName ?? "Saving And Loan" },
                    { "Format", upperFormat }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Microfinance/VCenterCollectionSheetPrintReport.cshtml"
                    : "Views/Report/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetPrintReport.cshtml";

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
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CenterCollectionSheetPrint report");
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