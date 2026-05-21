//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Extensions.Options;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Dtos.RequestDtos.Account;
//using NexgenCosysReport.Dtos.RequestDtos.Common;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Inteface.ServiceInterface.Account;
//using NexgenCosysReport.Inteface.ServiceInterface.Common;
//using NexgenCosysReport.Services.ReportService;
//using NexgenCosysReport.Utils.Enum;
//using NexgenCosysReport.Utils.Report;
//using System.Text.Json;


//namespace NexgenCosysReport.Controllers.Account
//{
//    [ApiController]
//    [Route("api/[controller]")]

//    public class SavingACWiseBalanceReportController : ControllerBase
//    {
//        private readonly IJsReportService _jsReportService;
//        private readonly ICommonHeaderRepository _commonHeaderRepository;
//        private readonly ISavingAcWiseBalance _savingAcWiseBalanceRepository;
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IWebHostEnvironment _webHostEnvironment;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<SavingACWiseBalanceReportController> _logger;

//        // ── Page size for this specific report ────────────────────────
//        private static readonly PageSizeSetting _pageSetting =
//         PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

//        //private static readonly PageSizeSetting _pageSetting =
//        //        PageSizeSetting.A3Landscape;

//        public SavingACWiseBalanceReportController(IJsReportService jsReportService, ICommonHeaderRepository commonHeaderRepository, ISavingAcWiseBalance savingAcWiseBalanceRepository, IWebHostEnvironment webHostEnvironment, IOptions<ReportSettings> reportSettings, ILogger<SavingACWiseBalanceReportController> logger, CustomHeaderResponse headerResponse)
//        {
//            _jsReportService = jsReportService;
//            _commonHeaderRepository = commonHeaderRepository;
//            _savingAcWiseBalanceRepository = savingAcWiseBalanceRepository;
//            _webHostEnvironment = webHostEnvironment;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _headerResponse = headerResponse;
//        }
//        [HttpPost]
//        public async Task<ActionResult> SavingAcWiseReport([FromBody] SavingAcWiseBalanceRequest request, [FromQuery] string format = "VIEW")
//        {
//            var reportName = "SavingACWiseBalanceReport";
//            var upperFormat = format.ToUpper();
//            var response = new GeneralResponse<ReportResponseDtos>();
//            try
//            {
//                if (request == null || !ModelState.IsValid)
//                {
//                    response.isValid = false;
//                    response.statusCode = StatusCodes.Status400BadRequest;
//                    response.message = "Invalid request data.";
//                    return BadRequest(response);
//                }

//                // ── Normalize filters: 0 or negative → -1 (all) ──────────────────────
//                request.DepositId = request.DepositId <= 0 ? -1 : request.DepositId;
//                request.CollectorId = request.CollectorId <= 0 ? -1 : request.CollectorId;
//                request.MemberGroupId = request.MemberGroupId <= 0 ? -1 : request.MemberGroupId;
//                request.CollectionCenterId = request.CollectionCenterId <= 0 ? -1 : request.CollectionCenterId;
//                if (request.BranchSelected == "0") request.BranchSelected = "-1";




//                var reportKey = ReportUtils.GenerateReportKey(request, reportName) + $"_{upperFormat}";

//                ReportExportHelper.LogCacheState(upperFormat, reportKey,
//                    _jsReportService.TryGetCachedHtml(reportKey), _logger);

//                // -- NO DB CALL — serving from cache ---------------------------------------
//                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(reportKey))
//                {
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        reportKey,
//                        upperFormat,
//                        reportName,
//                        _jsReportService,
//                        _logger,
//                        _pageSetting
//                        );
//                }


//                var commonHeaderTask = _commonHeaderRepository.GetCommonHeaders();
//                var spDataTask = _savingAcWiseBalanceRepository.GetSavingAcWiseBalanceAsync(request);

//                await Task.WhenAll(commonHeaderTask, spDataTask);
//                var commonHeader = await commonHeaderTask;
//                var spData = await spDataTask;

//                if (!spData.Any())
//                {
//                    response.isValid = false;
//                    response.statusCode = 404;
//                    response.message = "No data found";
//                    return NotFound(response);
//                }

//                if (spData.Count > 0)
//                {
//                    // Multiply to reach target row count (e.g., 1500)
//                    int targetRows = 30000;
//                    int repeatCount = (int)Math.Ceiling((double)targetRows / spData.Count);
//                    spData = Enumerable.Repeat(spData, repeatCount)
//                                       .SelectMany(x => x)
//                                       .Take(targetRows)
//                                       .ToList();
//                }

//                var webRoot = ReportUtils.GetWebRootPath(
//                     _webHostEnvironment, _reportSettings);

//                await Task.Run(() => ReportUtils.ConvertUniqueImagesToBase64Async(
//                   commonHeader,
//                   nameof(CommonHeader.CompanyLogo),
//                   webRoot));

//                var reportData = new Dictionary<string, object>
//                {
//                    { "HeaderDataSet",commonHeader },
//                    {"SavingAcWiseBalanceDataset", spData },
//                    {"Format",   upperFormat},
//                    {"SameCompanyName",request.SameCompanyName},
//                    { "BranchName",          request.BranchName},
//                    {"TillDate",request.TillDate},

//                };

//                var renderRazorpage = await Task.Run(() =>
//                   _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                       reportKey: reportKey,
//                       reportPath: "Views/Report/SavingAcWiseBalance.cshtml",
//                       data: reportData));

//                if (upperFormat == "VIEW")
//                {
//                    var pdfBytes = await Task.Run(() =>
//                        _jsReportService.ExportReportToFormatAsync(renderRazorpage, "PDF", reportKey, _pageSetting));


//                    // Log PDF size
//                    var fileSizeBytes = pdfBytes.Length;
//                    var fileSizeKB = fileSizeBytes / 1024.0;
//                    var fileSizeMB = fileSizeKB / 1024.0;

//                    _logger.LogInformation(
//                        "Generated PDF Size: {Bytes} bytes ({KB:F2} KB, {MB:F2} MB)",
//                        fileSizeBytes,
//                        fileSizeKB,
//                        fileSizeMB
//                    );

//                    var totalPages = JsReportService.CountPdfPages(pdfBytes);
//                    var pagination = new Pagination
//                    {
//                        currentPage = 1,
//                        totalPages = totalPages,
//                        totalRecord = spData.Count(),
//                        pageSize = 1,
//                        hasNextPage = totalPages > 1,
//                        hasPreviousPage = false
//                    };
//                    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//                    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

//                    Response.Headers.Append(
//                        "Content-Disposition",
//                        "inline; filename=\"MemberIdCardReport.pdf\"");

//                    return new FileContentResult(pdfBytes, "application/pdf");



//                }

//                return await ReportExportHelper.ExportFromCacheAsync(
//                    reportKey, upperFormat,
//                    reportName,
//                    _jsReportService, _logger, _pageSetting);


//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "An error occurred while processing your request.",
//                    error = ex.Message
//                });
//            }


//        }
//    }
//}





using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Common;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Account;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Services.ReportService;
using NexgenCosysReport.Utils.Enum;
using NexgenCosysReport.Utils.Report;
using System.Text.Json;

namespace NexgenCosysReport.Controllers.Account
{
    [ApiController]
    [Route("api/[controller]")]
    public class SavingACWiseBalanceReportController : ControllerBase
    {
        private readonly IJsReportService _jsReportService;
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
        private const int ChunkThreshold = 20000;
        private const int MaxParallelism = 2;

        private static readonly PageSizeSetting PageSetting =
            PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

        public SavingACWiseBalanceReportController(
            IJsReportService jsReportService,
            ICommonHeaderRepository commonHeaderRepository,
            ISavingAcWiseBalance savingAcRepo,
            IWebHostEnvironment env,
            IOptions<ReportSettings> reportSettings,
            ILogger<SavingACWiseBalanceReportController> logger,
            CustomHeaderResponse headerResponse)
        {
            _jsReportService = jsReportService;
            _commonHeaderRepository = commonHeaderRepository;
            _savingAcRepo = savingAcRepo;
            _env = env;
            _reportSettings = reportSettings;
            _logger = logger;
            _headerResponse = headerResponse;
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
                var reportKey = ReportUtils.GenerateReportKey(request, ReportName) + $"_{upperFormat}";

                ReportExportHelper.LogCacheState(upperFormat, reportKey,
                    _jsReportService.TryGetCachedHtml(reportKey, out _), _logger);

                // ── Try PDF cache early (VIEW) ────────────────────────────
                if (upperFormat == "VIEW" &&
                    _jsReportService.TryGetCachedPdf(reportKey, out var cachedPdf) &&
                    cachedPdf != null)
                {
                    return BuildPdfResponse(cachedPdf, totalRecords: -1);
                }

                // ── Non-VIEW cache hit ────────────────────────────────────
                if (upperFormat != "VIEW" &&
                    _jsReportService.TryGetCachedHtml(reportKey, out _))
                {
                    return await ReportExportHelper.ExportFromCacheAsync(
                        reportKey, upperFormat, ReportName,
                        _jsReportService, _logger, PageSetting);
                }

                // ── Fetch data in parallel ────────────────────────────────
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

                // ── Resolve images ────────────────────────────────────────
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

                // ── VIEW: PDF generation ──────────────────────────────────
                if (upperFormat == "VIEW")
                {
                    byte[] pdfBytes;

                    if (spData.Count > ChunkThreshold)
                    {
                        _logger.LogInformation(
                            "🔀 Large dataset ({Count}) — chunked mode (size={Size}, parallel={P})",
                            spData.Count, ChunkSize, MaxParallelism);

                        pdfBytes = await _jsReportService.ExportChunkedPdfAsync(
                            reportKey, ReportPath, reportData, RowsKey,
                            ChunkSize, PageSetting, MaxParallelism, ct);
                    }
                    else
                    {
                        var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                            reportKey, ReportPath, reportData, ct);

                        pdfBytes = await _jsReportService.ExportReportToFormatAsync(
                            html, "PDF", reportKey, PageSetting, ct);
                    }

                    return BuildPdfResponse(pdfBytes, spData.Count);
                }

                // ── Non-VIEW formats ──────────────────────────────────────
                await _jsReportService.RenderRazorToHtmlAndCacheAsync(
                    reportKey, ReportPath, reportData, ct);

                return await ReportExportHelper.ExportFromCacheAsync(
                    reportKey, upperFormat, ReportName,
                    _jsReportService, _logger, PageSetting);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⛔ Report request cancelled by client.");
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
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        private static void NormalizeFilters(SavingAcWiseBalanceRequest r)
        {
            if (r.DepositId <= 0) r.DepositId = -1;
            if (r.CollectorId <= 0) r.CollectorId = -1;
            if (r.MemberGroupId <= 0) r.MemberGroupId = -1;
            if (r.CollectionCenterId <= 0) r.CollectionCenterId = -1;
            if (r.BranchSelected == "0") r.BranchSelected = "-1";
        }

        private FileContentResult BuildPdfResponse(byte[] pdfBytes, int totalRecords)
        {
            var totalPages = JsReportService.CountPdfPages(pdfBytes);

            _logger.LogInformation("📄 Final PDF — {Pages} pages, {MB:F2} MB",
                totalPages, pdfBytes.Length / 1024.0 / 1024.0);

            var pagination = new Pagination
            {
                currentPage = 1,
                totalPages = totalPages,
                totalRecord = totalRecords,
                pageSize = 1,
                hasNextPage = totalPages > 1,
                hasPreviousPage = false
            };

            _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            Response.Headers.Append(
                "Content-Disposition",
                "inline; filename=\"MemberIdCardReport.pdf\"");

            return new FileContentResult(pdfBytes, "application/pdf");
        }


#if DEBUG
        /// <summary>
        /// TEST ONLY — multiplies real DB rows to simulate a large dataset.
        /// Compiled out automatically in Release builds — safe to leave in.
        /// </summary>
        private static List<T> InflateForLoadTest<T>(List<T> source, int targetRows)
        {
            if (source.Count == 0) return source;
            int repeatCount = (int)Math.Ceiling((double)targetRows / source.Count);
            return Enumerable.Repeat(source, repeatCount)
                             .SelectMany(x => x)
                             .Take(targetRows)
                             .ToList();
        }
#endif
    }
}


