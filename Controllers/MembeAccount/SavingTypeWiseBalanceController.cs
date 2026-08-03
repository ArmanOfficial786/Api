// Controllers/AccountOperation/SavingTypeWiseBalanceController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MembeAccount
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavingTypeWiseBalanceController : ControllerBase
    {
        private readonly ISavingTypeWiseBalance _savingTypeService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingTypeWiseBalanceController> _logger;

        public SavingTypeWiseBalanceController(
            ISavingTypeWiseBalance savingTypeService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            CustomHeaderResponse headerResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<SavingTypeWiseBalanceController> logger)
        {
            _savingTypeService = savingTypeService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _headerResponse = headerResponse;
            _reportSettings = reportSettings;
            _logger = logger;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] SavingTypeWiseBalanceRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "SavingTypeWiseBalance";
                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);
                }

                // Fetch data
                var dataTask = _savingTypeService.GetSavingTypeWiseBalance(request);
                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchId) && request.BranchId != "-1")
                {
                    branchIdForHeader = request.BranchId;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

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
                    { "IsNepali", request.IsNepali },
                    { "OpeningBalance", request.OpeningBalance },
                    { "PercentageBalance", request.PercentageBalance },
                    { "GroupByBranch", request.GroupByBranch },
                    { "GroupByCollectionCenter", request.GroupByCollectionCenter },
                    { "GroupByMemberGroup", request.GroupByMemberGroup },
                    { "ViewCollector", request.ViewCollector },
                    { "ViewDetail", request.ViewDetail },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/VisualReport/VSavingTypeWiseBalanceReport.cshtml"
                            : "Views/Report/MemberAC/SavingTypeWiseBalance.cshtml",
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
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SavingTypeWiseBalance report generation failed");
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}