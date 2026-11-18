using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.MemberAccount.SavingAccWiseReport
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavingTypeWiseIndividualBalanceController : ControllerBase
    {
        private readonly ISavingTypeWiseIndividualBalance _savingTypeService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingTypeWiseIndividualBalanceController> _logger;
        private readonly IReportFileResponse _reportFileResponse;



        public SavingTypeWiseIndividualBalanceController(
            ISavingTypeWiseIndividualBalance savingTypeService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<SavingTypeWiseIndividualBalanceController> logger,
            IReportFileResponse reportFileResponse)
        {
            _savingTypeService = savingTypeService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Portrait;


        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] SavingTypeWiseIndividualBalanceRequest request,
            [FromQuery] string format = "VIEW", CancellationToken ct = default)
        {
            try
            {
                var reportName = "SavingTypeWiseIndividualBalance";
                var upperFormat = format.ToUpper();
                //  var reportKey = ReportUtils.GenerateReportKey(request, reportName);

                var reportKey = ReportUtils.GenerateReportKey(request, reportName, request.VisualReport ? "VISUAL" : "");

                var cached = await ReportExportHelper.TryServeAsync(
                reportKey, upperFormat, reportName, pdfView: false,
                _jsReportService, _reportFileResponse, _logger, PageSetting, ct);
                if (cached != null) return cached;



                //if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                //{
                //    return await ReportExportHelper.ExportFromCacheAsync(
                //        reportKey, upperFormat, reportName,
                //        _jsReportService, _logger);
                //}

                var timer = System.Diagnostics.Stopwatch.StartNew();
                void Lap(string step) { _logger.LogInformation("{Step}: {Ms} ms", step, timer.ElapsedMilliseconds); timer.Restart(); }

                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" && !request.BranchId.Contains(','))
                {
                    branchIdForHeader = request.BranchId;
                }

                // Fetch data
                var dataTask = _savingTypeService.GetSavingTypeWiseIndividualBalance(request);
                Lap("Repository");
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");
                Lap("header");

                await Task.WhenAll(dataTask, headerTask);
                Lap("Repository + header");

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, message = "No data found" });
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot));

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalOpening", data.TotalOpening },
                    { "TotalDeposit", data.TotalDeposit },
                    { "TotalWithdraw", data.TotalWithdraw },
                    { "TotalBalance", data.TotalBalance },
                    { "TotalClosing", data.TotalClosing },
                    { "TotalRecords", data.TotalRecords },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.BranchName },
                    { "OpeningBalance", request.OpeningBalance },
                    { "PercentageBalance", request.PercentageBalance },
                    { "GroupByBranch", request.GroupByBranch },
                    { "GroupByCollectionCenter", request.GroupByCollectionCenter },
                    { "GroupByMemberGroup", request.GroupByMemberGroup },
                    { "ViewDetail", request.ViewDetail },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat },
                    { "PageSetting", PageSetting }
                };

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/VisualReport/VSavingTypeWiseIndividualBalanceReport.cshtml"
                            : "Views/Report/MemberAC/SavingAcWiseReport/SavingTypeWiseIndividualBalance.cshtml",
                        data: reportData));
                Lap("Razor render");

                if (upperFormat == "VIEW")
                {


                    var viewHtmlNormal = await _jsReportService.ExportReportToRawHtmlAsync(
                      htmlContent, reportKey, PageSetting, ct);
                    Lap("Export to raw HTML");
                    var Pagination = SheetHtml.CountSheets(viewHtmlNormal);
                    _reportFileResponse.AppendPaginationHeaders(Pagination, data.Rows.Count);
                    return Content(viewHtmlNormal, "text/html");
                    Lap("Return");


                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger, PageSetting, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SavingTypeWiseIndividualBalance report generation failed");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}