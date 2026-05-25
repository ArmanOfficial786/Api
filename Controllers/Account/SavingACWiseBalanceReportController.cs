using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Controllers.Account
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavingACWiseBalanceReportController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
        private readonly IPdfChunkService _pdfChunkService;
        private readonly IReportFileResponse _reportFileResponse;
        private readonly ICommonHeaderRepository _commonHeaderRepository;
        private readonly ISavingAcWiseBalance _savingAcRepo;
        private readonly CustomHeaderResponse _headerResponse;
        private readonly IWebHostEnvironment _env;
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILogger<SavingACWiseBalanceReportController> _logger;

        private const string ReportName = "SavingACWiseBalanceReport";
        private const string ReportPath = "Views/Report/SavingAcWiseBalance.cshtml";
        private const string RowsKey = "SavingAcWiseBalanceDataset";
        private const int ChunkSize = 1000;
        private const int ChunkThreshold = 20_000;
        private const int MaxParallelism = 2;

        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

        public SavingACWiseBalanceReportController(
            IJsReportService jsReportService,
            IPdfChunkService pdfChunkService,
            ICommonHeaderRepository commonHeaderRepository,
            ISavingAcWiseBalance savingAcRepo,
            IWebHostEnvironment env,
            IOptions<ReportSettings> reportSettings,
            ILogger<SavingACWiseBalanceReportController> logger,
            CustomHeaderResponse headerResponse,
            IReportFileResponse reportFileResponse)
        {
            _jsReportService = jsReportService;
            _pdfChunkService = pdfChunkService;
            _commonHeaderRepository = commonHeaderRepository;
            _savingAcRepo = savingAcRepo;
            _env = env;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
            _reportFileResponse = reportFileResponse;
        }

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
                var baseKey = ReportUtils.GenerateReportKey(request, ReportName) + $"_{upperFormat}";
                var chunkKey = ReportUtils.GenerateReportKey(request, ReportName); // format-independent

                ReportExportHelper.LogCacheState(upperFormat, baseKey,
                    _jsReportService.TryGetCachedHtml(baseKey, out _), _logger);

                // ── Small-PDF VIEW cache ──────────────────────────────────
                if (upperFormat == "VIEW" &&
                    _jsReportService.TryGetCachedPdf(baseKey, out var cachedPdf) &&
                    cachedPdf != null)
                {
                    return _reportFileResponse.BuildPdfResponse(cachedPdf, totalRecords: -1);
                }

                // ── Non-VIEW HTML cache (small datasets) ──────────────────
                if (upperFormat != "VIEW" &&
                    _jsReportService.TryGetCachedHtml(baseKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        baseKey, upperFormat, ReportName,
                        _jsReportService, _logger, PageSetting, ct);
                }

                // ── Fetch data ────────────────────────────────────────────
                var commonHeaderTask = _commonHeaderRepository.GetCommonHeaders();
                var spDataTask = _savingAcRepo.GetSavingAcWiseBalanceAsync(request);
                await Task.WhenAll(commonHeaderTask, spDataTask);

                var commonHeader = commonHeaderTask.Result;
                var spData = spDataTask.Result;

                if (spData is null || spData.Count == 0)
                    return NotFound(new GeneralResponse<ReportResponseDtos>
                    {
                        isValid = false,
                        statusCode = 404,
                        message = "No data found"
                    });


                spData = InflateForLoadTest(spData, targetRows: 50_000);

                _logger.LogInformation("📊 Total rows: {Count}", spData.Count);

                // ── Resolve logo ──────────────────────────────────────────
                var webRoot = ReportUtils.GetWebRootPath(_env, _reportSettings);
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    commonHeader, nameof(CommonHeader.CompanyLogo), webRoot);

                // ── Build report data ─────────────────────────────────────
                var reportData = new Dictionary<string, object>
                {
                    ["HeaderDataSet"] = commonHeader,
                    [RowsKey] = spData,
                    ["Format"] = upperFormat,
                    ["SameCompanyName"] = request.SameCompanyName,
                    ["BranchName"] = request.BranchName,
                    ["TillDate"] = request.TillDate,
                };

                // ═════════════════════════════════════════════════════════
                // LARGE DATASET — chunked path
                // ═════════════════════════════════════════════════════════
                if (spData.Count > ChunkThreshold)
                {
                    // Shared across VIEW and export formats
                    if (!_pdfChunkService.TryGetChunkedPdfPath(chunkKey, out var pdfPath) || pdfPath == null)
                    {
                        pdfPath = await _pdfChunkService.ExportChunkedPdfAsync(
                            chunkKey, ReportPath, reportData, RowsKey,
                            ChunkSize, PageSetting, MaxParallelism, ct);
                    }

                    return upperFormat == "VIEW"
                        ? _reportFileResponse.BuildPdfStreamResponse(pdfPath, spData.Count)
                        : ReportExportHelper.ExportFromDiskAsync(pdfPath, upperFormat, ReportName, _logger);
                }

                // ═════════════════════════════════════════════════════════
                // SMALL DATASET — standard path
                // ═════════════════════════════════════════════════════════
                var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    baseKey, ReportPath, reportData, ct);

                if (upperFormat == "VIEW")
                {
                    var pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                        html, "PDF", baseKey, PageSetting, ct);

                    return _reportFileResponse.BuildPdfResponse(pdfBytes, spData.Count);
                }

                return await ReportExportHelper.ExportFromCacheAsync(
                    baseKey, upperFormat, ReportName,
                    _jsReportService, _logger, PageSetting, ct);
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

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════════





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



