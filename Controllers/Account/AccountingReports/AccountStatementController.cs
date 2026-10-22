using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Account.AccountingReports
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountStatementController : ControllerBase
    {
        private const string ReportName = "AccountStatement";

        private sealed record StatementBundle(
            List<AccountStatementModelResponse> Statement,
            List<CashBankBalanceModelResponse> Balance);

        private readonly IAccountStatement _accountStatementService;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<AccountStatementController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public AccountStatementController(
            IAccountStatement accountStatementService,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<AccountStatementController> logger,
            IReportFileResponse reportFileResponse)
        {
            _accountStatementService = accountStatementService;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost("AccountStatementReport")]
        public async Task<IActionResult> GenerateReport(
            [FromBody] AccountStatementRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var userScope = User.FindFirst("UserId")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? "anonymous";

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userScope, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerBranch = request.SameCompanyName ? null : request.BranchSelected;
                var headerTask = _headers.GetAsync(headerBranch, ct);
                var dataTask = _dataCache.GetOrLoadAsync<StatementBundle>(dataKey, async () =>
                {
                    var statementTask = _accountStatementService.GetAccountStatementTypeAsync(request);
                    var balanceTask = _accountStatementService.GetCashAndBankBalanceOpeningClosingAsync(request);
                    await Task.WhenAll(statementTask, balanceTask);

                    var statement = (await statementTask).ToList();
                    if (statement.Count == 0) return null;
                    return new StatementBundle(statement, (await balanceTask).ToList());
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "AccountStatementDataSet", data.Statement },
                    { "CashBankBalanceDataSet", data.Balance },
                    { "HeaderDataSet", await headerTask },
                    { "TotalRecords", data.Statement.Count },
                    { "ReportType", request.ReportType },
                    { "TransactionType", request.TransactionType },
                    { "BranchName", request.BranchName },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "Format", upperFormat },
                    { "SameCompanyName", request.SameCompanyName },
                    { "VisualReport", request.VisualReport },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Account/VAccountStatementReport.cshtml"
                    : "Views/Report/Account/AccountingReport/AccountStatementReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Statement.Count,
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