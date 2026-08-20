// Controllers/AccountOperation/MemberAccountDetailController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.MembeAccount
{
    [Route("api/[controller]")]
    [ApiController]
    public class MemberAccountDetailController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IMemberAccountDetail _memberAccountDetail;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberAccountDetailController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Page size: A4 Portrait (240mm x 297mm)
        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(250, 297, PageUnit.mm, landscape: false);

        public MemberAccountDetailController(
            IJsReportService jsReportService,
            ICommonHeaderRepository commonHeaderRepository,
            IReportFileResponse reportFileResponse,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberAccountDetailController> logger,
            IWebHostEnvironment webHostEnvironment,
            IMemberAccountDetail memberAccountDetail)
        {
            _jsReportService = jsReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _reportFileResponse = reportFileResponse;
            _reportSettings = reportSettings;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _memberAccountDetail = memberAccountDetail;
        }

        [HttpPost()]
        public async Task<IActionResult> GenerateReport(
            [FromBody] MemberAccountDetailRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                // Validate Till Date
                if (string.IsNullOrEmpty(request.TillDate) || request.TillDate == "-1")
                {
                    return BadRequest(new { success = false, message = "Till Date is required" });
                }

                // Validate Branch selection
                if (string.IsNullOrEmpty(request.BranchIds) || request.BranchIds == "-1")
                {
                    if (!request.SameCompanyName)
                    {
                        return BadRequest(new { success = false, message = "Please select Branch Office" });
                    }
                }

                // Validate selected columns
                if (request.SelectedColumns == null || !request.SelectedColumns.Any())
                {
                    return BadRequest(new { success = false, message = "Please select at least one field to display" });
                }

                var upperFormat = format.ToUpper();
                var reportKey = ReportUtils.GenerateReportKey(request, "MemberAccountDetail");

                ReportExportHelper.LogCacheState(
                    upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                // Serve from cache if available
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("Serving MemberAccountDetail from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, "MemberAccountDetail",
                        _jsReportService, _logger, PageSetting, ct);
                }

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                // Fetch data
                var headerTask = _commonHeaderRepository.GetCommonHeaders();
                var memberTask = _memberAccountDetail.GetMemberAccountDetailReport(request, ct);
                await Task.WhenAll(memberTask, headerTask);

                var data = memberTask.Result;
                var headerData = headerTask.Result;

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
                    { "TotalDeposit", data.TotalDeposit },
                    { "TotalWithdraw", data.TotalWithdraw },
                    { "TotalBalance", data.TotalBalance },
                    { "HeaderDataSet", headerData },
                    { "TillDate", request.TillDate },
                    { "BranchName", request.SameCompanyName ? "Same Company" : request.BranchName },
                    { "MemberId", request.MemberId },
                    { "MemberName", request.MemberName },
                    { "Status", GetStatusLabel(request.Status) },
                    { "OrderBy", request.OrderBy },
                    { "EnableCollectionCenterGroup", request.EnableCollectionCenterGroup },
                    { "EnableMemberGroupGroup", request.EnableMemberGroupGroup },
                    { "SelectedColumns", request.SelectedColumns },
                    { "Format", upperFormat }
                };

                // Render HTML
                var htmlContent = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey: reportKey,
                    reportPath: request.VisualReport
                        ? "Views/VisualReport/AccountOperation/VMemberAccountDetailReport.cshtml"
                        : "Views/Report/MemberAC/MemberAccountDetail.cshtml",
                    data: reportData);

                // Handle VIEW format
                if (upperFormat == "VIEW")
                {
                    var viewHtml = await _jsReportService.ExportReportToRawHtmlAsync(
                        htmlContent, reportKey, ct);

                    _logger.LogInformation("VIEW — jsreport Html recipe, {Bytes:N0} chars", viewHtml.Length);
                    return Content(viewHtml, "text/html");
                }

                // Handle PDF format
                if (upperFormat == "PDF")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        htmlContent, "PDF", reportKey, PageSetting, ct);
                    return _reportFileResponse.BuildPdfResponse(pdfBytes);
                }

                // Handle other formats
                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, "MemberAccountDetail",
                    _jsReportService, _logger, PageSetting, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating MemberAccountDetail report");
                return StatusCode(500, new { success = false, message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        private string GetStatusLabel(int status)
        {
            return status switch
            {
                1 => "Opened",
                2 => "Closed",
                3 => "With Balance",
                4 => "Suspended",
                5 => "Disable",
                _ => "All"
            };
        }
    }
}