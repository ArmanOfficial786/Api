//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Enum;          // PageUnit
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;

//namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize]
//    public class LoanProvisionController : ControllerBase
//    {
//        private readonly ILoanProvisionRepository _repository;
//        private readonly ICommonHeaderRepository _commonHeaderRepository;
//        private readonly IJsReportService _jsReportService;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<LoanProvisionController> _logger;
//        private readonly IReportFileResponse _reportFileResponse;

//        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
//        private static readonly PageSizeSetting VPageSetting =
//            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

//        public LoanProvisionController(
//            ILoanProvisionRepository repository,
//            ICommonHeaderRepository commonHeaderRepository,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<LoanProvisionController> logger,
//            IReportFileResponse reportFileResponse)
//        {
//            _repository = repository;
//            _commonHeaderRepository = commonHeaderRepository;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _reportFileResponse = reportFileResponse;
//        }

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] LoanProvisionRequestDto request,
//            [FromQuery] string format = "VIEW",
//            CancellationToken ct = default)
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
//                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

//                if (request == null || !ModelState.IsValid)
//                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

//                var reportName = "LoanProvision";
//                var upperFormat = format.ToUpper();
//                var isVisual = request.VisualReport;

//                var page = isVisual ? VPageSetting : NPageSetting;

//                //var reportKey = ReportUtils.GenerateReportKey(
//                //    request, reportName, $"{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}");
//                // key = whole request (every filter) + view type + page size + user
//                var reportKey = ReportUtils.GenerateReportKey(
//                    request, reportName,
//                    $"{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}_U{userIdClaim}");


//                var cached = await ReportExportHelper.TryServeAsync(
//                 reportKey, upperFormat, reportName, pdfView: false,
//                 _jsReportService, _reportFileResponse, _logger, page, ct);
//                if (cached != null) return cached;

//                string? branchIdForHeader = null;
//                if (!string.IsNullOrEmpty(request.BranchIds) &&
//                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
//                {
//                    branchIdForHeader = request.BranchIds;
//                }

//                var dataTask = _repository.GetReportDataAsync(request);
//                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");
//                await Task.WhenAll(dataTask, headerTask);

//                var data = await dataTask;
//                var headerData = await headerTask;

//                if (!data.Rows.Any())
//                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

//                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
//                await ReportUtils.ConvertUniqueImagesToBase64Async(
//                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

//                var reportData = new Dictionary<string, object>
//                {
//                    { "Rows", data.Rows },
//                    { "TotalRecords", data.TotalRecords },
//                    { "TotalAmount", data.TotalAmount },
//                    { "TotalProvisionAmount", data.TotalProvisionAmount },
//                    { "TotalLoans", data.TotalLoans },
//                    { "HeaderDataSet", headerData },
//                    { "TillDate", data.TillDateBs ?? "" },
//                    { "BranchName", data.BranchName ?? "All" },
//                    { "MemberGroupName", data.MemberGroupName ?? "All" },
//                    { "PenaltyType", data.PenaltyType ?? "S" },
//                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
//                    { "Format", upperFormat },
//                    { "PageSetting", page }
//                };

//                string viewPath = isVisual
//                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanProvisionReport.cshtml"
//                    : "Views/Report/Loan/LoanAnalysisReports/LoanProvisionReport.cshtml";

//                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

//                if (upperFormat == "VIEW")
//                {
//                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
//                        htmlContent, reportKey, page, ct);
//                    _reportFileResponse.AppendPaginationHeaders(
//                        SheetHtml.CountSheets(viewHtml), data.Rows.Count);
//                    return Content(viewHtml, "text/html");
//                }

//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat, reportName, _jsReportService, _logger, page, ct);
//            }

//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating LoanProvision report");
//                return StatusCode(500, new
//                {
//                    message = ex.Message,
//                    inner = ex.InnerException?.Message,
//                    stack = ex.StackTrace
//                });
//            }
//        }
//    }
//}



// Controllers/Loan/LoanAnalysisReport/LoanProvisionController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;          // PageUnit
using NexgenCosysReport.Utils.Report;
using System.Diagnostics;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanProvisionController : ControllerBase
    {
        private const string ReportName = "LoanProvision";

        private readonly ILoanProvisionRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<LoanProvisionController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Portrait;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LoanProvisionController(
            ILoanProvisionRepository repository,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanProvisionController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanProvisionRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            var total = Stopwatch.StartNew();

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                // ── KEYS ──
                // data key: every filter in the request (TillDateBs, BranchIds, MemberGroupId, PenaltyType) + user.
                //           VisualReport is excluded, so normal and visual share one cached result.
                // html key: data key + view type + page size.
                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));

                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                // HTML hit: serve VIEW or export, no DB and no Razor
                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                // ── HEADER: cached per branch (query + logo conversion run once, not on every data miss) ──
                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                    branchIdForHeader = request.BranchIds;



                var headerTask = _dataCache.GetOrLoadAsync($"Header_{branchIdForHeader ?? "ALL"}", async () =>
                {
                    var headerData = await _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");
                    var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                    await ReportUtils.ConvertUniqueImagesToBase64Async(
                        headerData, nameof(CommonHeader.CompanyLogo), webRoot);
                    return headerData;
                }, ct);

                // ── DATA: loaded once, then reused by normal and visual until it expires ──
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    return loaded.Rows.Any() ? loaded : null;               // empty results are never cached
                }, ct);

                await Task.WhenAll(headerTask, dataTask);

                var headerRows = await headerTask;
                var data = await dataTask;

                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                // ── RENDER from the (possibly cached) data ──
                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalAmount", data.TotalAmount },
                    { "TotalProvisionAmount", data.TotalProvisionAmount },
                    { "TotalLoans", data.TotalLoans },
                    { "HeaderDataSet", headerRows! },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "All" },
                    { "PenaltyType", data.PenaltyType ?? "S" },
                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanProvisionReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanProvisionReport.cshtml";


                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);


                // VIEW (raw HTML + pagination headers) or export (PDF/Excel/...) from the cached HTML
                var result = await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.Rows.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);



                return result;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}