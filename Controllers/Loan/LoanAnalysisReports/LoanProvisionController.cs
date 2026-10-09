
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;
//using System.Text.Json;

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
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<LoanProvisionController> _logger;
//        private readonly IDateConverterService _dateConverter;

//        public LoanProvisionController(
//            ILoanProvisionRepository repository,
//            ICommonHeaderRepository commonHeaderRepository,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            CustomHeaderResponse headerResponse,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<LoanProvisionController> logger,
//            IDateConverterService dateConverter)
//        {
//            _repository = repository;
//            _commonHeaderRepository = commonHeaderRepository;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _headerResponse = headerResponse;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _dateConverter = dateConverter;
//        }

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] LoanProvisionRequestDto request,
//            [FromQuery] string format = "VIEW")
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


//                var reportName = "LoanProvision";
//                var upperFormat = format.ToUpper();
//                bool isVisual = request.VisualReport;

//                var reportKey = isVisual
//                    ? ReportUtils.GenerateReportKey(request, reportName, "VISUAL")
//                    : ReportUtils.GenerateReportKey(request, reportName);


//                ReportExportHelper.LogCacheState(upperFormat, reportKey,
//                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

//                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
//                {
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        reportKey, upperFormat,
//                        reportName,
//                        _jsReportService, _logger);
//                }

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
//                {
//                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });
//                }

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
//                    { "Format", upperFormat }
//                };

//                string viewPath = request.VisualReport
//                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanProvisionReport.cshtml"
//                    : "Views/Report/Loan/LoanAnalysisReports/LoanProvisionReport.cshtml";

//                var htmlContent = await Task.Run(() =>
//                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                        reportKey: reportKey,
//                        reportPath: viewPath,
//                        data: reportData));

//                if (upperFormat == "VIEW")
//                {
//                    if (isVisual)
//                    {
//                        var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(htmlContent, reportKey);
//                        return Content(viewHtml, "text/html");
//                    }

//                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
//                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
//                    var pagination = new Pagination
//                    {
//                        currentPage = 1,
//                        totalPages = totalPages,
//                        pageSize = 1,
//                        hasNextPage = totalPages > 1,
//                        hasPreviousPage = false,
//                        totalRecord = data.Rows.Count
//                    };

//                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
//                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");

//                    return new FileContentResult(pdfBytes, "application/pdf");
//                }
//                return await ReportExportHelper.ExportFromCacheAsync(
//                 reportKey, upperFormat,
//                 reportName,
//                 _jsReportService, _logger);
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
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

//new approach

//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Enum;          // PageUnit
//using NexgenCosysReport.Utils.Report;
//using System.Security.Claims;
//using System.Text.Json;

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
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<LoanProvisionController> _logger;
//        private readonly IDateConverterService _dateConverter;
//        private readonly IReportFileResponse _reportFileResponse;

//        // Visual report page only. Change it here: HTML view and PDF both follow it.
//        private static readonly PageSizeSetting PageSetting =
//            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

//        // Derived once from PageSetting (sheet size, margins, rows per page)
//        private static readonly SheetLayout Layout = SheetLayout.From(PageSetting);

//        public LoanProvisionController(
//            ILoanProvisionRepository repository,
//            ICommonHeaderRepository commonHeaderRepository,
//            IJsReportService jsReportService,
//            IWebHostEnvironment webHostEnvironment,
//            CustomHeaderResponse headerResponse,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<LoanProvisionController> logger,
//            IDateConverterService dateConverter,
//            IReportFileResponse reportFileResponse)
//        {
//            _repository = repository;
//            _commonHeaderRepository = commonHeaderRepository;
//            _jsReportService = jsReportService;
//            _webHostEnvironment = webHostEnvironment;
//            _headerResponse = headerResponse;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _dateConverter = dateConverter;
//            _reportFileResponse = reportFileResponse;
//        }

//        [HttpPost()]
//        public async Task<IActionResult> GenerateReport(
//            [FromBody] LoanProvisionRequestDto request,
//            [FromQuery] string format = "VIEW")
//        {
//            try
//            {
//                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
//                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
//                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

//                if (request == null || !ModelState.IsValid)
//                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

//                var reportName = "LoanProvision";
//                var upperFormat = format.ToUpper();
//                bool isVisual = request.VisualReport;

//                // Page size is part of the visual key, so changing PageSetting invalidates old cached HTML/PDF.
//                var reportKey = isVisual
//                    ? ReportUtils.GenerateReportKey(request, reportName, $"VISUAL_{Layout.CacheTag}")
//                    : ReportUtils.GenerateReportKey(request, reportName);

//                // Only visual reports use PageSetting; normal reports keep their current defaults.
//                PageSizeSetting? exportPage = isVisual ? PageSetting : null;

//                ReportExportHelper.LogCacheState(upperFormat, reportKey,
//                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

//                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
//                {
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        reportKey, upperFormat, reportName, _jsReportService, _logger, exportPage);
//                }

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
//                    { "SheetLayout", Layout }              // read by the visual view only
//                };

//                string viewPath = isVisual
//                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanProvisionReport.cshtml"
//                    : "Views/Report/Loan/LoanAnalysisReports/LoanProvisionReport.cshtml";

//                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                    reportKey: reportKey, reportPath: viewPath, data: reportData);

//                if (upperFormat == "VIEW")
//                {
//                    // ═════════ VISUAL: raw HTML, pages = fixed sheets ═════════
//                    if (isVisual)
//                    {
//                        var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
//                            htmlContent, reportKey, PageSetting);

//                        var sheets = SheetLayout.CountSheets(viewHtml);   // same count the PDF will have
//                        var vPagination = new Pagination
//                        {
//                            currentPage = 1,
//                            totalPages = sheets,
//                            pageSize = 1,
//                            hasNextPage = sheets > 1,
//                            hasPreviousPage = false,
//                            totalRecord = data.Rows.Count
//                        };
//                        _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//                        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(vPagination));
//                        return Content(viewHtml, "text/html");
//                    }

//                    // ═════════ NORMAL: PDF + iText page count (unchanged) ═════════
//                    //var pdfBytes = await _jsReportService.ExportReportToFormatAsync(htmlContent, "PDF", reportKey);
//                    //var totalPages = JsReportService.CountPdfPages(pdfBytes);
//                    //var pagination = new Pagination
//                    //{
//                    //    currentPage = 1,
//                    //    totalPages = totalPages,
//                    //    pageSize = 1,
//                    //    hasNextPage = totalPages > 1,
//                    //    hasPreviousPage = false,
//                    //    totalRecord = data.Rows.Count
//                    //};

//                    //_headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//                    //Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
//                    //Response.Headers.Append("Content-Disposition", $"inline; filename=\"{reportName}.pdf\"");
//                    //return new FileContentResult(pdfBytes, "application/pdf");

//                    var viewHtmlNormal = await _jsReportService.ExportReportToRawHtmlAsync(
//                   htmlContent, reportKey, PageSetting);
//                    var Pagination = SheetLayout.CountSheets(viewHtmlNormal);
//                    _reportFileResponse.AppendPaginationHeaders(Pagination, data.Rows.Count);
//                    return Content(viewHtmlNormal, "text/html");

//                }

//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat, reportName, _jsReportService, _logger, exportPage);
//            }
//            catch (ArgumentException ex)
//            {
//                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
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



//latest new appraoch

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Enum;          // PageUnit
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.Loan.LoanAnalysisReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoanProvisionController : ControllerBase
    {
        private readonly ILoanProvisionRepository _repository;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
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
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<LoanProvisionController> logger,
            IReportFileResponse reportFileResponse)
        {
            _repository = repository;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
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
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "Unauthorized" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                var reportName = "LoanProvision";
                var upperFormat = format.ToUpper();
                var isVisual = request.VisualReport;

                var page = isVisual ? VPageSetting : NPageSetting;

                var reportKey = ReportUtils.GenerateReportKey(
                    request, reportName, $"{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}");

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName, _jsReportService, _logger, page, ct);
                }

                string? branchIdForHeader = null;
                if (!string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }

                var dataTask = _repository.GetReportDataAsync(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");
                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                    return NotFound(new { success = false, StatusCode = 400, message = "No data found" });

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalAmount", data.TotalAmount },
                    { "TotalProvisionAmount", data.TotalProvisionAmount },
                    { "TotalLoans", data.TotalLoans },
                    { "HeaderDataSet", headerData },
                    { "TillDate", data.TillDateBs ?? "" },
                    { "BranchName", data.BranchName ?? "All" },
                    { "MemberGroupName", data.MemberGroupName ?? "All" },
                    { "PenaltyType", data.PenaltyType ?? "S" },
                    { "PenaltyTypeName", data.PenaltyTypeName ?? "Schedule Wise" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = isVisual
                    ? "Views/VisualReport/Loan/LoanAnalysisReports/VLoanProvisionReport.cshtml"
                    : "Views/Report/Loan/LoanAnalysisReports/LoanProvisionReport.cshtml";

                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey, reportPath: viewPath, data: reportData, ct: ct);

                if (upperFormat == "VIEW")
                {
                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
                        htmlContent, reportKey, page, ct);
                    _reportFileResponse.AppendPaginationHeaders(
                        SheetHtml.CountSheets(viewHtml), data.Rows.Count);
                    return Content(viewHtml, "text/html");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName, _jsReportService, _logger, page, ct);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, StatusCode = 400, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating LoanProvision report");
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