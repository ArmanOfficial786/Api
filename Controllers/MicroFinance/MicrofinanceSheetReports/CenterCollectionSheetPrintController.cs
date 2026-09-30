// Controllers/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetPrintController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceSheetReports;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

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
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<CenterCollectionSheetPrintController> _logger;

        private static readonly PageSizeSetting PageSetting =
          PageSizeSetting.Custom(250, 297, PageUnit.mm, landscape: false);

        public CenterCollectionSheetPrintController(
            ICenterCollectionSheetPrintRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<CenterCollectionSheetPrintController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] CenterCollectionSheetPrintRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
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
               { "TotalSaving1Balance", data.TotalSaving1Balance },
               { "TotalSaving2Balance", data.TotalSaving2Balance },
               { "TotalSaving3Balance", data.TotalSaving3Balance },
               { "TotalSaving4Balance", data.TotalSaving4Balance },
               { "TotalLoan1RemPrinciple", data.TotalLoan1RemPrinciple },
               { "TotalLoan1PayablePri", data.TotalLoan1PayablePri },
               { "TotalLoan1PayableInt", data.TotalLoan1PayableInt },
               { "TotalLoan2RemPrinciple", data.TotalLoan2RemPrinciple },
               { "TotalLoan2PayablePri", data.TotalLoan2PayablePri },
               { "TotalLoan2PayableInt", data.TotalLoan2PayableInt },
               { "TotalLoan3RemPrinciple", data.TotalLoan3RemPrinciple },
               { "TotalLoan3PayablePri", data.TotalLoan3PayablePri },
               { "TotalLoan3PayableInt", data.TotalLoan3PayableInt },
               { "TotalLoan4RemPrinciple", data.TotalLoan4RemPrinciple },
               { "TotalLoan4PayablePri", data.TotalLoan4PayablePri },
               { "TotalLoan4PayableInt", data.TotalLoan4PayableInt },
               { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
               { "TillDate", data.TillDateBs ?? "" },
               { "TillDateAd", data.TillDateAd ?? "" },
               { "SheetType", data.SheetType ?? "A" },
               { "SheetTypeName", data.SheetTypeName ?? "Saving And Loan" },
               { "Format", upperFormat }
               };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/Microfinance/VCenterCollectionSheetPrintReport.cshtml"
                    : "Views/Report/MicroFinance/MicrofinanceSheetReports/CenterCollectionSheetPrintReport.cshtml";

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: viewPath,
                        data: reportData));


                if (upperFormat == "VIEW")
                {
                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
                        htmlContent, reportKey, ct);

                    _logger.LogInformation("VIEW — jsreport Html recipe, {Bytes:N0} chars", viewHtml.Length);
                    return Content(viewHtml, "text/html");
                }

                // Handle PDF format
                if (upperFormat == "PDF")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        htmlContent, "PDF", reportKey, PageSetting, ct);
                    return _reportFileResponse.BuildPdfResponse(pdfBytes);
                }

                // Handle other formats
                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, "MemberAccountDetail",
                    _jsReportService, _logger, PageSetting, ct); ;
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