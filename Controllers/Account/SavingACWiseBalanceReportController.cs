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




//                var baseKey = ReportUtils.GeneratebaseKey(request, reportName) + $"_{upperFormat}";

//                ReportExportHelper.LogCacheState(upperFormat, baseKey,
//                    _jsReportService.TryGetCachedHtml(baseKey), _logger);

//                // -- NO DB CALL — serving from cache ---------------------------------------
//                if (upperFormat != "VIEW" && _jsReportService.TryGetCachedHtml(baseKey))
//                {
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        baseKey,
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
//                       baseKey: baseKey,
//                       reportPath: "Views/Report/SavingAcWiseBalance.cshtml",
//                       data: reportData));

//                if (upperFormat == "VIEW")
//                {
//                    var pdfBytes = await Task.Run(() =>
//                        _jsReportService.ExportReportToFormatAsync(renderRazorpage, "PDF", baseKey, _pageSetting));


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
//                    baseKey, upperFormat,
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
//        private readonly ISavingAcWiseBalance _savingAcRepo;
//        private readonly CustomHeaderResponse _headerResponse;
//        private readonly IWebHostEnvironment _env;
//        private readonly IOptions<ReportSettings> _reportSettings;
//        private readonly ILogger<SavingACWiseBalanceReportController> _logger;

//        private const string ReportName = "SavingACWiseBalanceReport";
//        private const string ReportPath = "Views/Report/SavingAcWiseBalance.cshtml";
//        private const string RowsKey = "SavingAcWiseBalanceDataset";
//        private const int ChunkSize = 1000;
//        private const int ChunkThreshold = 20000;
//        private const int MaxParallelism = 2;

//        private static readonly PageSizeSetting PageSetting =
//            PageSizeSetting.Custom(240, 297, PageUnit.mm, landscape: false);

//        public SavingACWiseBalanceReportController(
//            IJsReportService jsReportService,
//            ICommonHeaderRepository commonHeaderRepository,
//            ISavingAcWiseBalance savingAcRepo,
//            IWebHostEnvironment env,
//            IOptions<ReportSettings> reportSettings,
//            ILogger<SavingACWiseBalanceReportController> logger,
//            CustomHeaderResponse headerResponse)
//        {
//            _jsReportService = jsReportService;
//            _commonHeaderRepository = commonHeaderRepository;
//            _savingAcRepo = savingAcRepo;
//            _env = env;
//            _reportSettings = reportSettings;
//            _logger = logger;
//            _headerResponse = headerResponse;
//        }

//        [HttpPost]
//        public async Task<ActionResult> SavingAcWiseReport(
//            [FromBody] SavingAcWiseBalanceRequest request,
//            [FromQuery] string format = "VIEW",
//            CancellationToken ct = default)
//        {
//            if (request == null || !ModelState.IsValid)
//                return BadRequest(new GeneralResponse<ReportResponseDtos>
//                {
//                    isValid = false,
//                    statusCode = StatusCodes.Status400BadRequest,
//                    message = "Invalid request data."
//                });

//            try
//            {
//                NormalizeFilters(request);

//                var upperFormat = format.ToUpperInvariant();
//                var baseKey = ReportUtils.GenerateReportKey(request, ReportName) + $"_{upperFormat}";

//                ReportExportHelper.LogCacheState(upperFormat, baseKey,
//                    _jsReportService.TryGetCachedHtml(baseKey, out _), _logger);

//                // ── Try PDF cache early (VIEW) ────────────────────────────
//                if (upperFormat == "VIEW" &&
//                    _jsReportService.TryGetCachedPdf(baseKey, out var cachedPdf) &&
//                    cachedPdf != null)
//                {
//                    return BuildPdfResponse(cachedPdf, totalRecords: -1);
//                }

//                // ── Non-VIEW cache hit ────────────────────────────────────
//                if (upperFormat != "VIEW" &&
//                    _jsReportService.TryGetCachedHtml(baseKey, out _))
//                {
//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        baseKey, upperFormat, ReportName,
//                        _jsReportService, _logger, PageSetting);
//                }

//                // ── Fetch data in parallel ────────────────────────────────
//                var commonHeaderTask = _commonHeaderRepository.GetCommonHeaders();
//                var spDataTask = _savingAcRepo.GetSavingAcWiseBalanceAsync(request);
//                await Task.WhenAll(commonHeaderTask, spDataTask);

//                var commonHeader = commonHeaderTask.Result;
//                var spData = spDataTask.Result;

//                if (spData is null || spData.Count == 0)
//                    return NotFound(new GeneralResponse<ReportResponseDtos>
//                    {
//                        isValid = false,
//                        statusCode = 404,
//                        message = "No data found"
//                    });


//                spData = InflateForLoadTest(spData, targetRows: 50_000);

//                _logger.LogInformation("📊 Total rows: {Count}", spData.Count);

//                // ── Resolve images ────────────────────────────────────────
//                var webRoot = ReportUtils.GetWebRootPath(_env, _reportSettings);
//                await ReportUtils.ConvertUniqueImagesToBase64Async(
//                    commonHeader, nameof(CommonHeader.CompanyLogo), webRoot);

//                // ── Build report data ─────────────────────────────────────
//                var reportData = new Dictionary<string, object>
//                {
//                    ["HeaderDataSet"] = commonHeader,
//                    [RowsKey] = spData,
//                    ["Format"] = upperFormat,
//                    ["SameCompanyName"] = request.SameCompanyName,
//                    ["BranchName"] = request.BranchName,
//                    ["TillDate"] = request.TillDate,
//                };

//                // ── VIEW: PDF generation ──────────────────────────────────
//                if (upperFormat == "VIEW")
//                {
//                    byte[] pdfBytes;

//                    if (spData.Count > ChunkThreshold)
//                    {
//                        var chunkedPdfKey = ReportUtils.GenerateReportKey(request, ReportName);

//                        if (!_jsReportService.TryGetChunkedPdfPath(chunkedPdfKey, out var pdfPath) || pdfPath == null)
//                        {
//                            pdfPath = await _jsReportService.ExportChunkedPdfAsync(
//                                chunkedPdfKey, ReportPath, reportData, RowsKey,
//                                ChunkSize, PageSetting, MaxParallelism, ct);
//                        }

//                        return BuildPdfStreamResponse(pdfPath, spData.Count);
//                    }
//                    else
//                    {
//                        var html = await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                            baseKey, ReportPath, reportData, ct);

//                        pdfBytes = await _jsReportService.ExportReportToFormatAsync(
//                            html, "PDF", baseKey, PageSetting, ct);
//                    }

//                    return BuildPdfResponse(pdfBytes, spData.Count);
//                }

//                // ── Non-VIEW formats ──────────────────────────────────────
//                // For large datasets, use the chunked PDF path even for EXCEL/HTML downloads
//                if (spData.Count > ChunkThreshold)
//                {
//                    // Use a format‑independent key to share the chunked PDF across VIEW, PDF, EXCEL, etc.
//                    var chunkedPdfKey = ReportUtils.GenerateReportKey(request, ReportName); // no format suffix

//                    if (!_jsReportService.TryGetChunkedPdfPath(chunkedPdfKey, out var pdfPath) || pdfPath == null || !System.IO.File.Exists(pdfPath))
//                    {
//                        pdfPath = await _jsReportService.ExportChunkedPdfAsync(
//                            chunkedPdfKey, ReportPath, reportData, RowsKey,
//                            ChunkSize, PageSetting, MaxParallelism, ct);
//                    }
//                    // Stream the existing PDF from disk – no re‑chunking
//                    return ReportExportHelper.ExportFromDiskAsync(
//                        pdfPath, upperFormat, ReportName, _logger);
//                }
//                else
//                {
//                    // Small dataset – normal HTML rendering + export
//                    await _jsReportService.RenderRazorToHtmlAndCacheAsync(
//                        baseKey, ReportPath, reportData, ct);

//                    return await ReportExportHelper.ExportFromCacheAsync(
//                        baseKey, upperFormat, ReportName,
//                        _jsReportService, _logger, PageSetting, ct);
//                }
//            }
//            catch (OperationCanceledException)
//            {
//                _logger.LogWarning("⛔ Report request cancelled by client.");
//                return StatusCode(499, new { message = "Request cancelled." });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ SavingAcWiseReport failed");
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "An error occurred while processing your request.",
//                    error = ex.Message
//                });
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // HELPERS
//        // ═══════════════════════════════════════════════════════════════════

//        private static void NormalizeFilters(SavingAcWiseBalanceRequest r)
//        {
//            if (r.DepositId <= 0) r.DepositId = -1;
//            if (r.CollectorId <= 0) r.CollectorId = -1;
//            if (r.MemberGroupId <= 0) r.MemberGroupId = -1;
//            if (r.CollectionCenterId <= 0) r.CollectionCenterId = -1;
//            if (r.BranchSelected == "0") r.BranchSelected = "-1";
//        }

//        //Build pdf response for small size pdf
//        private FileContentResult BuildPdfResponse(byte[] pdfBytes, int totalRecords)
//        {

//            var totalPages = JsReportService.CountPdfPages(pdfBytes);

//            _logger.LogInformation("📄 Final PDF — {Pages} pages, {MB:F2} MB",
//                totalPages, pdfBytes.Length / 1024.0 / 1024.0);

//            var pagination = new Pagination
//            {
//                currentPage = 1,
//                totalPages = totalPages,
//                totalRecord = totalRecords,
//                pageSize = 1,
//                hasNextPage = totalPages > 1,
//                hasPreviousPage = false
//            };

//            _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

//            Response.Headers.Append(
//                "Content-Disposition",
//                "inline; filename=\"MemberIdCardReport.pdf\"");

//            return new FileContentResult(pdfBytes, "application/pdf");
//        }

//        private FileStreamResult BuildPdfStreamResponse(string pdfPath, int totalRecords)
//        {
//            // ✅ Count pages BEFORE opening the DeleteOnClose stream.
//            // FileOptions.DeleteOnClose on Windows prevents subsequent opens
//            // unless the second caller also uses FileShare.Delete — so we
//            // read the page count while the file is still fully accessible.
//            int totalPages = CountPdfPagesFromFile(pdfPath);

//            _logger.LogInformation("📄 Chunked PDF — {Pages} pages, path={Path}", totalPages, pdfPath);

//            // ✅ Now open the streaming handle. DeleteOnClose removes the temp
//            // file automatically once ASP.NET finishes sending the response.
//            var fs = new FileStream(
//                pdfPath,
//                FileMode.Open,
//                FileAccess.Read,
//                FileShare.Read,          // no second reader needed after this point
//                bufferSize: 81_920,
//                options: FileOptions.Asynchronous
//                       //| FileOptions.DeleteOnClose
//                       | FileOptions.SequentialScan);

//            var pagination = new Pagination
//            {
//                currentPage = 1,
//                totalPages = totalPages,
//                totalRecord = totalRecords,
//                pageSize = 1,
//                hasNextPage = totalPages > 1,
//                hasPreviousPage = false
//            };

//            _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
//            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
//            Response.Headers.Append("Content-Disposition",
//                "inline; filename=\"SavingAcWiseBalance.pdf\"");

//            return new FileStreamResult(fs, "application/pdf");
//        }

//        private static int CountPdfPagesFromFile(string pdfPath)
//        {
//            try
//            {
//                using var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
//                using var reader = new iText.Kernel.Pdf.PdfReader(fs);
//                using var doc = new iText.Kernel.Pdf.PdfDocument(reader);
//                return doc.GetNumberOfPages();
//            }
//            catch { return 1; }
//        }


//#if DEBUG
//        /// <summary>
//        /// TEST ONLY — multiplies real DB rows to simulate a large dataset.
//        /// Compiled out automatically in Release builds — safe to leave in.
//        /// </summary>
//        private static List<T> InflateForLoadTest<T>(List<T> source, int targetRows)
//        {
//            if (source.Count == 0) return source;
//            int repeatCount = (int)Math.Ceiling((double)targetRows / source.Count);
//            return Enumerable.Repeat(source, repeatCount)
//                             .SelectMany(x => x)
//                             .Take(targetRows)
//                             .ToList();
//        }
//#endif
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

        /// <summary>In-memory PDF — small enough to fit in cache.</summary>
        //private FileContentResult BuildPdfResponse(byte[] pdfBytes, int totalRecords)
        //{
        //    var totalPages = JsReportService.CountPdfPages(pdfBytes);

        //    _logger.LogInformation("📄 PDF — {Pages} pages, {MB:F2} MB",
        //        totalPages, pdfBytes.Length / 1024.0 / 1024.0);

        //    AppendPaginationHeaders(totalPages, totalRecords);
        //    Response.Headers.Append("Content-Disposition",
        //        "inline; filename=\"SavingAcWiseBalance.pdf\"");

        //    return new FileContentResult(pdfBytes, "application/pdf");
        //}

        ///// <summary>Large chunked PDF — streamed from disk without loading into memory.</summary>
        //private FileStreamResult BuildPdfStreamResponse(string pdfPath, int totalRecords)
        //{
        //    var totalPages = CountPdfPagesFromFile(pdfPath);
        //    _logger.LogInformation("📄 Chunked PDF — {Pages} pages, path={Path}", totalPages, pdfPath);

        //    var fs = new FileStream(
        //        pdfPath,
        //        FileMode.Open,
        //        FileAccess.Read,
        //        FileShare.Read,
        //        bufferSize: 81_920,
        //        options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        //    AppendPaginationHeaders(totalPages, totalRecords);
        //    Response.Headers.Append("Content-Disposition",
        //        "inline; filename=\"SavingAcWiseBalance.pdf\"");

        //    return new FileStreamResult(fs, "application/pdf");
        //}

        //private void AppendPaginationHeaders(int totalPages, int totalRecords)
        //{
        //    var pagination = new Pagination
        //    {
        //        currentPage = 1,
        //        totalPages = totalPages,
        //        totalRecord = totalRecords,
        //        pageSize = 1,
        //        hasNextPage = totalPages > 1,
        //        hasPreviousPage = false
        //    };
        //    _headerResponse.SetResponseHeaders(true, 200, "Report generated successfully.");
        //    Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));
        //}

        //private static int CountPdfPagesFromFile(string pdfPath)
        //{
        //    try
        //    {
        //        using var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        //        using var reader = new iText.Kernel.Pdf.PdfReader(fs);
        //        using var doc = new iText.Kernel.Pdf.PdfDocument(reader);
        //        return doc.GetNumberOfPages();
        //    }
        //    catch { return 1; }
        //}

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



