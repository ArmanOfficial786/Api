// Controllers/AccountOperation/MemberPenaltyDepositWithdrawController.cs
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

namespace NexgenCosysReport.Controllers.AccountOperation
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberPenaltyDepositWithdrawController : ControllerBase
    {
        // Page size setting: A4 Portrait (240mm x 297mm)
        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

        private readonly IMemberPenaltyDepositWithdraw _penaltyDepositWithdrawService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberPenaltyDepositWithdrawController> _logger;

        public MemberPenaltyDepositWithdrawController(
            IMemberPenaltyDepositWithdraw penaltyDepositWithdrawService,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberPenaltyDepositWithdrawController> logger,
            CustomHeaderResponse headerResponse)
        {
            _penaltyDepositWithdrawService = penaltyDepositWithdrawService;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberPenaltyDepositWithdrawRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                // Validate dates
                if (string.IsNullOrEmpty(request.FromDate) || request.FromDate == "-1")
                {
                    return BadRequest(new { success = false, message = "From Date is required" });
                }

                if (string.IsNullOrEmpty(request.ToDate) || request.ToDate == "-1")
                {
                    return BadRequest(new { success = false, message = "To Date is required" });
                }

                // Validate branch selection
                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                {
                    if (!request.SameCompanyName)
                    {
                        return BadRequest(new { success = false, message = "Please select Branch Office" });
                    }
                }

                // Validate amount
                if (request.Amount < 0)
                {
                    return BadRequest(new { success = false, message = "Amount must be greater than or equal to 0" });
                }

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberPenaltyDepositWithdrawReport");

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                // Serve from cache if available
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("Serving MemberPenaltyDepositWithdrawReport from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, "MemberPenaltyDepositWithdrawReport",
                        _jsReportService, _logger, PageSetting, ct);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                // Fetch data
                var dataTask = _penaltyDepositWithdrawService.GetMemberPenaltyDepositWithdrawReport(request);

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

                string GetTransactionTypeLabel(int type) => type switch
                {
                    1 => "Penalty",
                    2 => "Deposit",
                    3 => "Withdraw",
                    4 => "Balance",
                    _ => "Penalty"
                };

                // Build report data
                var reportData = new Dictionary<string, object>
                {
                    { "Rows", data.Rows },
                    { "TotalRecords", data.TotalRecords },
                    { "TotalPenalty", data.TotalPenalty },
                    { "TotalDeposit", data.TotalDeposit },
                    { "TotalWithdraw", data.TotalWithdraw },
                    { "TotalBalance", data.TotalBalance },
                    { "HeaderDataSet", headerData },
                    { "FromDate", request.FromDate },
                    { "ToDate", request.ToDate },
                    { "BranchName", request.SameCompanyName ? "Same Company" : request.BranchName },
                    { "TransactionType", GetTransactionTypeLabel(request.TransactionType) },
                    { "TransactionTypeValue", request.TransactionType },
                    { "Amount", request.Amount },
                    { "OrderBy", request.OrderBy },
                    { "Format", upperFormat }
                };

                // Render HTML
                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: request.VisualReport
                        ? "Views/VisualReport/AccountOperation/VMemberPenaltyDepositWithdrawReport.cshtml"
                        : "Views/Report/MemberAC/MemberPenaltyDepositWithdraw.cshtml",
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
                    Response.Headers.Append("Content-Disposition", $"inline; filename=\"{"MemberPenaltyDepositWithdrawReport"}.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, "MemberPenaltyDepositWithdrawReport",
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MemberPenaltyDepositWithdraw report generation failed");
                return StatusCode(500, new { success = false, message = ex.Message, inner = ex.InnerException?.Message });
            }
        }
    }
}