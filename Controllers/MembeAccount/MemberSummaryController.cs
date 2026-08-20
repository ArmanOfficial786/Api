// Controllers/AccountOperation/MemberSummaryController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.MembeAccount
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberSummaryController : ControllerBase
    {
        // Page size setting: 240mm x 297mm (A4 Portrait)
        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(250, 297, PageUnit.mm, landscape: false);

        private readonly IMemberSummary _memberSummaryService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberSummaryController> _logger;

        public MemberSummaryController(
            IMemberSummary memberSummaryService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberSummaryController> logger,
            CustomHeaderResponse headerResponse)
        {
            _memberSummaryService = memberSummaryService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberSummaryRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                // Validate till date
                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
                {
                    return BadRequest(new { success = false, message = "Till Date is required" });
                }

                // Validate branch selection
                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                {
                    if (!request.SameCompanyName)
                    {
                        return BadRequest(new { success = false, message = "Please select Branch Office" });
                    }
                }

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberSummaryReport");

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                // Serve from cache if available
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("Serving MemberSummaryReport from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, "MemberSummaryReport",
                        _jsReportService, _logger, PageSetting, ct);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                // Fetch data
                var dataTask = _memberSummaryService.GetMemberSummaryReport(request);

                string? branchIdForHeader = null;
                if (!request.SameCompanyName && !string.IsNullOrEmpty(request.BranchIds) &&
                    request.BranchIds != "-1" && !request.BranchIds.Contains(','))
                {
                    branchIdForHeader = request.BranchIds;
                }
                var headerTask = _commonHeaderRepository.GetCommonHeaders(branchIdForHeader ?? "");

                await Task.WhenAll(dataTask, headerTask);

                var data = await dataTask;
                var headerData = await headerTask;

                if (!data.Rows.Any())
                {
                    return NotFound(new { success = false, message = "No data found" });
                }

                // Convert company logo to base64
                var header = headerData.FirstOrDefault();
                if (header != null && !string.IsNullOrEmpty(header.CompanyLogo))
                {
                    var companyLogoBase64 = await ReportUtils.ReadCommonImageAsBase64Async(
                        webRoot, header.CompanyLogo, _logger);
                    header.CompanyLogo = companyLogoBase64;
                }

                // Build report data
                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalShareAmount", data.TotalShareAmount },
                    { "TotalSaving", data.TotalSaving },
                    { "TotalLoan", data.TotalLoan },
                    { "GrandTotal", data.GrandTotal },
                    { "SavingTypeTotals", data.SavingTypeTotals },
                    { "HeaderDataSet", headerData },
                    { "TillDate", request.TillDate },
                    { "BranchName", request.SameCompanyName ? "Same Company" : request.BranchName },
                    { "CollectionCenter", request.CollectionCenterId ?? "All" },
                    { "MemberGroup", request.MemberGroupId ?? "All" },
                    { "EnableCollectionCenterGroup", request.EnableCollectionCenterGroup },
                    { "EnableMemberGroupGroup", request.EnableMemberGroupGroup },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                // Render HTML
                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: request.VisualReport
                        ? "Views/VisualReport/AccountOperation/VMemberSummaryReport.cshtml"
                        : "Views/Report/MemberAC/MemberSummary.cshtml",
                    data: reportData);

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
                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{"MemberSummaryReport"}.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, "MemberSummaryReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemberSummary report generation failed");
                return StatusCode(500, new { success = false, message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}