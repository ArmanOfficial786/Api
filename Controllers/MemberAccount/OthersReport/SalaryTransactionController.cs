using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport;
using NexgenCosysReport.Utils.Caching;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Security.Claims;

namespace NexgenCosysReport.Controllers.MemberAccount.OthersReport
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SalaryTransactionController : ControllerBase
    {
        private readonly ISalaryTransactionRepository _repository;
        private readonly IReportHeaderProvider _headers;
        private readonly IJsReportService _jsReportService;
        private readonly IReportDataCache _dataCache;
        private readonly ILogger<SalaryTransactionController> _logger;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IDateConverterService _dateConverter;

        private static readonly PageSizeSetting NPageSetting = PageSizeSetting.A4Landscape;
        private static readonly PageSizeSetting VPageSetting =
            PageSizeSetting.Custom(300, 297, PageUnit.mm, landscape: true);

        public SalaryTransactionController(
            ISalaryTransactionRepository repository,
            IReportHeaderProvider headers,
            IJsReportService jsReportService,
            IReportDataCache dataCache,
            ILogger<SalaryTransactionController> logger,
            IReportFileResponse reportFileResponse,
            IDateConverterService dateConverter)
        {
            _repository = repository;
            _headers = headers;
            _jsReportService = jsReportService;
            _dataCache = dataCache;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
            _dateConverter = dateConverter;
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(
            [FromBody] SalaryTransactionRequestDto request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            var reportName = $"SalaryTransaction_{(request?.ReportType == 1 ? "Summary" : "Detail")}";

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out _))
                    return NotFound(new { success = false, StatusCode = 401, message = "User not authenticated" });

                if (request == null || !ModelState.IsValid)
                    return BadRequest(new { success = false, StatusCode = 400, message = "Invalid request" });

                if (string.IsNullOrEmpty(request.FromDateBs) || request.FromDateBs == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "From date is required" });
                if (string.IsNullOrEmpty(request.ToDateBs) || request.ToDateBs == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "To date is required" });

                var fromDate = await _dateConverter.NepaliToEnglishAsync(request.FromDateBs);
                var toDate = await _dateConverter.NepaliToEnglishAsync(request.ToDateBs);
                if (fromDate > toDate)
                    return BadRequest(new { success = false, StatusCode = 400, message = "From date cannot be greater than To date" });

                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                    return BadRequest(new { success = false, StatusCode = 400, message = "Please select at least one branch" });

                var upperFormat = format.ToUpperInvariant();
                var isVisual = request.VisualReport;
                var page = isVisual ? VPageSetting : NPageSetting;

                var dataKey = ReportCacheKey.ForData(
                    request, reportName, userIdClaim, nameof(request.VisualReport));
                var reportKey = $"{dataKey}_{(isVisual ? "VISUAL" : "NORMAL")}_{page.CacheTag}";

                var cached = await ReportExportHelper.TryServeAsync(
                    reportKey, upperFormat, reportName, pdfView: request.VisualReport,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
                if (cached != null) return cached;

                var officeIdClaim = User.FindFirst("OfficeId")?.Value;
                var branchIdForHeader = long.TryParse(officeIdClaim, out var officeId) ? officeId.ToString() : "";

                var headerTask = _headers.GetAsync(branchIdForHeader, ct);
                var dataTask = _dataCache.GetOrLoadAsync(dataKey, async () =>
                {
                    var loaded = await _repository.GetReportDataAsync(request);
                    if (loaded == null || loaded.Rows == null || !loaded.Rows.Any())
                        return null;
                    return loaded;
                }, ct);
                await Task.WhenAll(headerTask, dataTask);

                var data = await dataTask;
                if (data == null)
                    return NotFound(new { success = false, StatusCode = 404, message = "No salary transactions found for the selected criteria" });

                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalSalaryAmount", data.TotalSalaryAmount },
                    { "TotalTdsAmount", data.TotalTdsAmount },
                    { "TotalAllowanceAmount", data.TotalAllowanceAmount },
                    { "TotalPFFO", data.TotalPFFO },
                    { "TotalPFFS", data.TotalPFFS },
                    { "TotalOverTimeSalary", data.TotalOverTimeSalary },
                    { "TotalNetSalary", data.TotalNetSalary },
                    { "HeaderDataSet", await headerTask },
                    { "FromDate", request.FromDateBs },
                    { "ToDate", request.ToDateBs },
                    { "BranchNames", request.BranchName ?? "All Branches" },
                    { "OrderBy", $"{request.OrderBy}" },
                    { "ReportType", request.ReportType },
                    { "TransferOn", request.TransferOn ?? "A" },
                    { "StaffSelection", request.StaffSelection ?? "O" },
                    { "SelectedStaffName", data.SelectedStaffName ?? "" },
                    { "Format", upperFormat },
                    { "PageSetting", page }
                };

                string viewPath = request.VisualReport
                    ? "Views/VisualReport/VSalaryTransactionReport.cshtml"
                    : "Views/Report/MemberAC/OthersReport/SalaryTransactionReport.cshtml";

                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: viewPath,
                    data: reportData, ct: ct);

                return await ReportExportHelper.CompleteAsync(
                    html, reportKey, upperFormat, reportName, pdfView: request.VisualReport,
                    totalRecords: data.Rows.Count,
                    _jsReportService, _reportFileResponse, _logger, page, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating {Report} report", reportName);
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}