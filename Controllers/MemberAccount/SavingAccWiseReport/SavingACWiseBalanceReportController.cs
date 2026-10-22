







// SavingACWiseBalanceReportController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.MemberAccount.SavingAccWiseReport
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavingACWiseBalanceReportController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly IProgressivePdfService _progressiveService;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly ISavingAcWiseBalance _savingAcRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingACWiseBalanceReportController> _logger;

        private const string ReportName = "SavingACWiseBalanceReport";
        private const string ReportPath = "Views/Report/MemberAC/SavingAcWiseReport/SavingAcWiseBalance.cshtml";
        private const string RowsKey = "SavingAcWiseBalanceDataset";

        /// <summary>
        /// Datasets larger than this use the progressive path for VIEW requests.
        /// Export formats (XLSX, PDF download) always use the standard path.
        /// </summary>
        private const int ProgressiveThreshold = 20_000;

        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

        public SavingACWiseBalanceReportController(
            IJsReportService jsReportService,
            IProgressivePdfService progressiveService,
            ICommonHeaderRepository commonHeaderRepository,
            ISavingAcWiseBalance savingAcRepo,
            IWebHostEnvironment env,
            IOptions<ReportSettings> reportSettings,
            ILogger<SavingACWiseBalanceReportController> logger,
            IReportFileResponse reportFileResponse)
        {
            _jsReportService = jsReportService;
            _progressiveService = progressiveService;
            _commonHeaderRepository = commonHeaderRepository;
            _savingAcRepo = savingAcRepo;
            _env = env;
            _reportSettings = reportSettings;
            _logger = logger;
            _reportFileResponse = reportFileResponse;
        }

        // ── POST — initial render ─────────────────────────────────────────
        [HttpPost]
        public async Task<ActionResult> SavingAcWiseReport(
            [FromBody] SavingAcWiseBalanceRequest request,
            [FromQuery] string format = "VIEW",
            CancellationToken ct = default)
        {
            if (request == null || !ModelState.IsValid)
                return BadRequest(new GeneralResponse<ReportResponseDtos>
                {
                    isValid = false,
                    statusCode = StatusCodes.Status400BadRequest,
                    message = "Invalid request data."
                });

            try
            {
                NormalizeFilters(request);
                var upperFormat = format.ToUpperInvariant();
                var baseKey = ReportUtils.GenerateReportKey(request, ReportName);

                // ── Cache hits (small-PDF path only) ─────────────────────
                if (upperFormat == "VIEW" &&
                    _jsReportService.TryGetCachedPdf(baseKey, out var cachedPdf) &&
                    cachedPdf != null)
                    return _reportFileResponse.BuildPdfResponse(cachedPdf, totalRecords: -1);

                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(baseKey, out _))
                    return await ReportExportHelper.ExportFromCacheAsync(
                        baseKey, upperFormat, ReportName, _jsReportService, _logger, PageSetting, ct);

                // ── Fetch data ────────────────────────────────────────────
                var commonHeaderTask = _commonHeaderRepository.GetCommonHeaders();
                var spDataTask = _savingAcRepo.GetSavingAcWiseBalanceAsync(request);
                await Task.WhenAll(commonHeaderTask, spDataTask);

                var commonHeader = commonHeaderTask.Result;
                var spData = spDataTask.Result;

                if (spData == null || spData.Count == 0)
                    return NotFound(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 404,
                        message = "No data found"
                    });

#if DEBUG
                spData = InflateForLoadTest(spData, targetRows: 50_000);
#endif
                _logger.LogInformation("📊 Total rows: {Count}", spData.Count);

                var webRoot = ReportUtils.GetWebRootPath(_env, _reportSettings);
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    commonHeader, nameof(CommonHeader.CompanyLogo), webRoot);

                var reportData = new Dictionary<string, object>
                {
                    ["HeaderDataSet"] = commonHeader,
                    [RowsKey] = spData,
                    ["Format"] = upperFormat,
                    ["SameCompanyName"] = request.SameCompanyName,
                    ["BranchName"] = request.BranchName,
                    ["TillDate"] = request.TillDate,
                };

                // ── Progressive path (large VIEW) ─────────────────────────
                if (upperFormat == "VIEW" && spData.Count > ProgressiveThreshold)
                {
                    // Chunk sizing is fully owned by the service (auto-calibrated).
                    // Controller only decides WHEN to use progressive — not HOW.
                    var job = await _progressiveService.StartAsync(
                        ReportPath, reportData, RowsKey, PageSetting, ct);

                    // Return the first-chunk PDF immediately with progress metadata
                    var (bytes, _) = await _progressiveService.GetSnapshotAsync(job.JobId, ct);

                    AppendProgressionHeaders(Response, job);
                    return File(bytes, "application/pdf");
                }

                // ── Standard path ─────────────────────────────────────────
                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    baseKey, ReportPath, reportData, ct);

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        html, "PDF", baseKey, PageSetting, ct);
                    return _reportFileResponse.BuildPdfResponse(pdfBytes, spData.Count);
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    baseKey, upperFormat, ReportName, _jsReportService, _logger, PageSetting, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⛔ Request cancelled.");
                return StatusCode(499, new { message = "Request cancelled." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SavingAcWiseReport failed");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while processing your request.",
                    error = ex.Message
                });
            }
        }

        // ── GET — poll for updated PDF ────────────────────────────────────
        [HttpGet("progressive/{jobId}")]
        public async Task<IActionResult> GetProgressivePdf(string jobId, CancellationToken ct)
        {
            try
            {
                var (bytes, job) = await _progressiveService.GetSnapshotAsync(jobId, ct);

                AppendProgressionHeaders(Response, job);
                return File(bytes, "application/pdf");
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Job not found or expired." });
            }
            catch (FileNotFoundException)
            {
                // First chunk not yet written — tell client to retry shortly
                return StatusCode(202, new { message = "Still rendering first chunk, retry shortly." });
            }
        }

        // ── Private helpers ───────────────────────────────────────────────

        private static void AppendProgressionHeaders(HttpResponse response, ProgressivePdfJob job)
        {
            var pagination = new
            {
                progressive = true,
                jobId = job.JobId,
                pagesReady = job.PagesReady,
                estimatedPages = job.EstimatedTotalPages,
                completedChunks = job.CompletedChunks,
                totalChunks = job.TotalChunks,
                isComplete = job.IsComplete,
                hasError = job.HasError,
                errorMessage = job.ErrorMessage
            };

            response.Headers["X-Pagination"] =
                System.Text.Json.JsonSerializer.Serialize(pagination);
            response.Headers["X-Pages-Ready"] = job.PagesReady.ToString();
            response.Headers["X-Is-Complete"] = job.IsComplete.ToString();
            response.Headers["X-Total-Chunks"] = job.TotalChunks.ToString();
            response.Headers["X-Completed-Chunks"] = job.CompletedChunks.ToString();
            response.Headers["X-Size-Bytes"] = job.CurrentSizeBytes.ToString();
            response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            response.Headers["Access-Control-Expose-Headers"] =
                "X-Pagination, X-Pages-Ready, X-Is-Complete, X-Total-Chunks, " +
                "X-Completed-Chunks, X-Size-Bytes, Content-Disposition";
        }

        private static void NormalizeFilters(SavingAcWiseBalanceRequest r)
        {
            if (r.DepositId <= 0) r.DepositId = -1;
            if (r.CollectorId <= 0) r.CollectorId = -1;
            if (r.MemberGroupId <= 0) r.MemberGroupId = -1;
            if (r.CollectionCenterId <= 0) r.CollectionCenterId = -1;
            if (r.BranchSelected == "0") r.BranchSelected = "-1";
        }

#if DEBUG
        private static List<T> InflateForLoadTest<T>(List<T> source, int targetRows)
        {
            if (source.Count == 0) return source;
            int repeat = (int)Math.Ceiling((double)targetRows / source.Count);
            return Enumerable.Repeat(source, repeat).SelectMany(x => x).Take(targetRows).ToList();
        }
#endif
    }
}