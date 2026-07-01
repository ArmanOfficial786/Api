using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.Member;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.Member;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Member
{
    [ApiController]
    [Route("api/[controller]")]
    public class MemberDetailsSummaryController : ControllerBase
    {
        private readonly IMemberDetailsSummary _memberDetailsSummary;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly IJsReportService _jsReportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<MemberDetailsSummaryController> _logger;

        public MemberDetailsSummaryController(
            IMemberDetailsSummary memberDetailsSummary,
            ICommonHeaderRepository commonHeaderRepository,
            IJsReportService jsReportService,
            IWebHostEnvironment webHostEnvironment,
            IOptions<ReportSettings> reportSettings,
            ILogger<MemberDetailsSummaryController> logger,
            CustomHeaderResponse headerResponse)
        {
            _memberDetailsSummary = memberDetailsSummary;
            _commonHeaderRepository = commonHeaderRepository;
            _jsReportService = jsReportService;
            _webHostEnvironment = webHostEnvironment;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
        }

        [HttpPost]
        public async Task<ActionResult> GenerateReport(          // ✅ ActionResult, no generic
            [FromBody] MemberDetailsSummaryRequest request,
            [FromQuery] string format = "VIEW")
        {
            try
            {
                var reportName = "MemberDetailsSummary";
                var upperFormat = format.ToUpper();

                // ── Validation ────────────────────────────────────────────────
                if (request == null || !ModelState.IsValid || request.MemberRegistrationId <= 0)
                    return BadRequest(new { success = false, message = "Invalid request. MemberRegistrationId is required." });

                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                // ── Cache hit — skip DB for non-VIEW ──────────────────────────
                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    _logger.LogInformation("✅ NO DB CALL — serving from cache");
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, reportName,
                        _jsReportService, _logger);
                }

                // ── Parallel DB calls ─────────────────────────────────────────
                var summaryTask = _memberDetailsSummary.GetMemberDetailsSummary(request);
                var headerTask = _commonHeaderRepository.GetCommonHeaders();
                await Task.WhenAll(summaryTask, headerTask);

                var summaryData = await summaryTask;
                var headerData = await headerTask;

                // ── Member existence check ────────────────────────────────────
                if (summaryData.MemberInfo == null)
                    return NotFound(new { success = false, message = "Member not found" });

                var webRoot = ReportUtils.GetWebRootPath(_webHostEnvironment, _reportSettings);

                // ── Convert company logo to Base64 ────────────────────────────
                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
                    headerData,
                    nameof(CommonHeader.CompanyLogo),
                    webRoot));

                // ── Report data dictionary ────────────────────────────────────
                var reportData = new Dictionary<string, object>
                {
                    { "MemberDetail",    summaryData.MemberInfo         },
                    { "ShareAccounts",   summaryData.ShareAccounts       },
                    { "SavingAccounts",  summaryData.SavingAccounts      },
                    { "LoanIssues",      summaryData.LoanIssues          },
                    { "GroupGuarantees", summaryData.GroupGuarantees     },
                    { "TotalShare",      summaryData.TotalShareRecords   },
                    { "TotalSaving",     summaryData.TotalSavingRecords  },
                    { "TotalLoan",       summaryData.TotalLoanRecords    },
                    { "TotalGuarantee",  summaryData.TotalGuaranteeRecords },
                    { "HeaderDataSet",   headerData                     },
                    { "FromDate",        request.FromDate               },
                    { "ToDate",          request.ToDate                 },
                    { "Format",          upperFormat                    },
                };

                // ── Razor render + cache HTML ─────────────────────────────────
                var htmlContent = await Task.Run(() =>
                    _jsReportService.RenderRazorToHtmlAndCacheAsync(
                        reportKey: reportKey,
                        reportPath: request.VisualReport
                            ? "Views/VisualReport/Member/VMemberDetailsSummaryReport.cshtml"
                            : "Views/Report/Member/MemberDetailsSummaryReport.cshtml",
                        data: reportData));

                // ── VIEW — return binary PDF ──────────────────────────────────
                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        htmlContent, "PDF", reportKey);
                    var totalPages = JsReportService.CountPdfPages(pdfBytes);

                    var pagination = new Pagination
                    {
                        currentPage = 1,
                        totalPages = totalPages,
                        totalRecord = 0,
                        pageSize = 1,
                        hasNextPage = totalPages > 1,
                        hasPreviousPage = false
                    };

                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
                    Response.Headers.Append("Content-Disposition", "inline; filename=\"MemberDetailSummary.pdf\"");

                    return new FileContentResult(pdfBytes, "application/pdf");
                }

                // ── EXPORT — binary blob from cache ───────────────────────────
                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, reportName,
                    _jsReportService, _logger);
            }
            catch (Exception ex)
            {
                _headerResponse.SetResponseHeaders(false, 500, $"Failed to generate report: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred.",
                    error = ex.Message,
                });
            }
        }
    }
}