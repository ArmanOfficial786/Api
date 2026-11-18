using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.OtherReports
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CashAndBankBalanceController : ControllerBase
    {
        private const string ReportName = "CashAndBankBalance";

        // one Razor view for both English and Nepali: the language is picked inside the view from the NepaliReport flag
        private const string ViewPath = "Views/Report/Account/OtherReports/CashAndBankBalanceReport.cshtml";

        private readonly ICashAndBankBalanceRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<CashAndBankBalanceController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        // 4 columns, so A4 portrait is enough
        private static readonly PageSizeSetting PageSetting = PageSizeSetting.A4Portrait;

        public CashAndBankBalanceController(
            ICashAndBankBalanceRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<CashAndBankBalanceController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] CashAndBankBalanceRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var upperFormat = format.ToUpperInvariant();
                var page = PageSetting;

                // the DB data is cached once and reused by every export format
                var dataKey = ReportCacheKey.ForData(request, ReportName, userIdClaim);
                var reportKey = $"{dataKey}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // the header shows one branch only when a single numeric branch is selected (same rule as before)
                var branchIdForHeader = "";
                if (!string.IsNullOrEmpty(request.BranchId) &&
                    request.BranchId != "-1" &&
                    request.BranchId != "string" &&
                    long.TryParse(request.BranchId, out _))
                {
                    branchIdForHeader = request.BranchId;
                }

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null ||
                        (!loaded.BankRows.Any() && !loaded.TellerRows.Any() && !loaded.CashRows.Any()))
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "BankRows", data.BankRows },
                    { "TellerRows", data.TellerRows },
                    { "CashRows", data.CashRows },
                    { "BankBalanceOutput", data.BankBalanceOutput },
                    { "CashBalanceOutput", data.CashBalanceOutput },
                    { "TellerBalance", data.TellerBalance },
                    { "FinalBankAmount", data.FinalBankAmount },
                    { "FinalCashAmount", data.FinalCashAmount },
                    { "HeaderDataSet", await headerTask },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "OrderBy", request.OrderBy ?? "" },
                    { "NepaliReport", data.NepaliReport },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: ViewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.BankRows.Count + data.TellerRows.Count + data.CashRows.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", ReportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}