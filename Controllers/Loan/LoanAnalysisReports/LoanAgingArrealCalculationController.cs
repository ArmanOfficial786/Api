//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Enum;
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;

//namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize]
//    public class LoanAgingArrealCalculationController : ControllerBase
//    {
//        private readonly ILoanAgingArrealCalculationRepository _repository;
//        private readonly ICommonHeaderRepository _commonHeaderRepository;
//        private readonly IJsReportService _jsReportService;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IReportFileResponse _reportFileResponse;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<LoanAgingArrealCalculationController> _logger;




//        public LoanAgingArrealCalculationController(
//            ILoanAgingArrealCalculationRepository repository,
//            ICommonHeaderRepository commonHeaderRepository,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            CustomHeaderResponse headerResponse,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<LoanAgingArrealCalculationController> logger,
//            IReportFileResponse reportFileResponse)
//        {
//            _repository = repository;
//            _commonHeaderRepository = commonHeaderRepository;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _headerResponse = headerResponse;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _reportFileResponse = reportFileResponse;
//        }

//        private static readonly PageSizeSetting NPageSetting =
//            PageSizeSetting.Custom(500, 420, PageUnit.mm, landscape: true);
//        private static readonly PageSizeSetting VPageSetting =
//            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] LoanAgingArrealCalculationRequestDto request,
//            [FromQuery] string format = "VIEW",
//            CancellationToken ct = default)
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
//                {
//                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });
//                }

//                if (request == null || !ModelState.IsValid)
//                {
//                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });
//                }

//                var reportName = "LoanAgingArrealCalculation";
//                var upperFormat = format.ToUpper();
//                var isVisual = request.VisualReport;
//                var page = isVisual ? VPageSetting : NPageSetting;

//                //var reportKey = ReportUtils.GenerateReportKey(request, reportName);
//                var reportKey = ReportUtils.GenerateReportKey(
//                request, reportName, $"{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}");

//                var cached = await ReportExportHelper.TryServeAsync(
//                  reportKey, upperFormat, reportName, pdfView: false,
//                  _jsReportService, _reportFileResponse, _logger, page, ct);
//                if (cached != null) return cached;

//                string? branchIdForHeader = null;
//                if (!string.IsNullOrEmpty(request.BranchIds) &&
//                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
//                {
//                    branchIdForHeader = request.BranchIds;
//                }
//                var timer = System.Diagnostics.Stopwatch.StartNew();
//                void Lap(string step) { _logger.LogInformation("{Step}: {Ms} ms", step, timer.ElapsedMilliseconds); timer.Restart(); }

//                var dataTask = _repository.GetReportDataAsync(request);
//                Lap("Repository");

//                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");
//                Lap("header");

//                await Task.WhenAll(dataTask, headerTask);
//                Lap("Repository + header");
//                var data = await dataTask;
//                _logger.LogInformation("Razor render: {Ms} ms", timer.ElapsedMilliseconds);
//                var headerData = await headerTask;

//                if (!data.Rows.Any())
//                {
//                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
//                }

//                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

//                await ReportUtils.ConvertUniqueImagesToBase64Async(
//                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);
//                Lap("Logo to base64");

//                var reportData = new Dictionary<string, object>
//                {
//                    { "Rows", data.Rows },
//                    { "TotalRecords", data.TotalRecords },
//                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
//                    { "TotalBalanceAmount", data.TotalBalanceAmount },
//                    { "TotalGoodloan", data.TotalGoodloan },
//                    { "TotalArrear", data.TotalArrear },
//                    { "TotalArrearFor0", data.TotalArrearFor0 },
//                    { "TotalArrearfrm1to365", data.TotalArrearfrm1to365 },
//                    { "TotalArreargrtthan365", data.TotalArreargrtthan365 },
//                    { "TotalGoodAmt", data.TotalGoodAmt },
//                    { "TotalFrm1to365", data.TotalFrm1to365 },
//                    { "TotalGrtthan365", data.TotalGrtthan365 },
//                    { "TotalProvision", data.TotalProvision },
//                    { "TotalRescheduleAmt", data.TotalRescheduleAmt },
//                    { "HeaderDataSet", headerData ?? new List<CommonHeader>() },
//                    { "TillDate", data.TillDateBs ?? "" },
//                    { "BranchName", data.BranchName ?? "All" },
//                    { "MemberGroupName", data.MemberGroupName ?? "All" },
//                    { "PenaltyType", data.PenaltyType ?? "S" },
//                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
//                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
//                    { "CollectorName", data.CollectorName ?? "" },
//                    { "Enable1To30Days", data.Enable1To30Days },
//                    { "EnableCollectionCenter", request.EnableCollectionCenter },
//                    { "OrderBy", data.OrderBy ?? "-1" },
//                    { "Format", upperFormat },
//                    { "PageSetting", page }
//                };

//                string viewPath = isVisual
//                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanAgingArrealCalculation.cshtml"
//                    : "Views/Report/Loan/LoanAnalysisReports/LoanAgingArrealCalculationReport.cshtml";


//                var htmlContent = await Task.Run(() =>
//                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                        reportKey: reportKey,
//                        reportPath: viewPath,
//                        data: reportData));
//                Lap("Razor render");

//                if (upperFormat == "VIEW")
//                {

//                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
//                      htmlContent, reportKey, page, ct);
//                    Lap("Export to raw HTML");
//                    _reportFileResponse.AppendPaginationHeaders(
//                        SheetHtml.CountSheets(viewHtml), data.TotalRecords);
//                    return Content(viewHtml, "text/html");

//                }



//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat, "MemberDetailReport",
//                    _jsReportService, _logger, page, ct);
//            }

//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating LoanAgingArrealCalculation report");
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







// Controllers/Loan/LoanAnalysisReports/LoanAgingArrealCalculationController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanAgingArrealCalculationController : ControllerBase
    {
        private const string ReportName = "LoanAgingArrealCalculation";

        private readonly ILoanAgingArrealCalculationRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<LoanAgingArrealCalculationController> _logger;
        private readonly IReportFileResponse _reportFileResponse;

        private static readonly PageSizeSetting NPageSetting =
            PageSizeSetting.Custom(500, 420, PageUnit.mm, landscape: true);
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public LoanAgingArrealCalculationController(
            ILoanAgingArrealCalculationRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<LoanAgingArrealCalculationController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] LoanAgingArrealCalculationRequestDto request,
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
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;


                var dataKey = ReportCacheKey.ForData(
                    request, ReportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";


                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, ReportName, pdfView: false,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var headerTask = _headers.GetAsync(request.BranchIds, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    return loaded.Rows.Any() ? loaded : null;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalLoanIssueAmount", data.TotalLoanIssueAmount },
                    { "TotalBalanceAmount", data.TotalBalanceAmount },
                    { "TotalGoodloan", data.TotalGoodloan },
                    { "TotalArrear", data.TotalArrear },
                    { "TotalArrearFor0", data.TotalArrearFor0 },
                    { "TotalArrearfrm1to365", data.TotalArrearfrm1to365 },
                    { "TotalArreargrtthan365", data.TotalArreargrtthan365 },
                    { "TotalGoodAmt", data.TotalGoodAmt },
                    { "TotalFrm1to365", data.TotalFrm1to365 },
                    { "TotalGrtthan365", data.TotalGrtthan365 },
                    { "TotalProvision", data.TotalProvision },
                    { "TotalRescheduleAmt", data.TotalRescheduleAmt },
                    { "HeaderDataSet", await headerTask },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "All" },
                    { "PenaltyType", data.PenaltyType ?? "S" },
                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
                    { "CollectionCenterName", data.CollectionCenterName ?? "" },
                    { "CollectorName", data.CollectorName ?? "" },
                    { "Enable1To30Days", data.Enable1To30Days },
                    { "EnableCollectionCenter", request.EnableCollectionCenter },
                    { "OrderBy", data.OrderBy ?? "-1" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                var viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanAgingArrealCalculation.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanAgingArrealCalculationReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, ReportName, pdfView: false,
                    totalRecords: data.TotalRecords,
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