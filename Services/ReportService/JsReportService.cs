//using iText.Kernel.Pdf;
//using jsreport.AspNetCore;
//using jsreport.Types;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Abstractions;
//using Microsoft.AspNetCore.Mvc.ModelBinding;
//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.Extensions.Caching.Memory;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Utils.Report;

//namespace NexgenCosysReport.Services.ReportService
//{
//    /// <summary>
//    /// Report Service - Handles rendering Razor views to HTML and exporting to various formats
//    /// 
//    /// Workflow:
//    /// 1. RenderRazorToHtmlAndCacheAsync() - Render Razor .cshtml → HTML → cache
//    /// 2. ExportReportToFormatAsync()      - Export cached HTML to PDF/Excel/PNG
//    ///
//    /// Supported Export Formats:
//    ///   - PDF, VIEW (renders as PDF in browser)
//    ///   - HTML
//    ///   - EXCEL, XLSX
//    ///   - PNG
//    /// </summary>
//    public class JsReportService : IJsReportService
//    {
//        private readonly ILogger<JsReportService> _logger;
//        private readonly IJsReportMVCService _jsReportMVCService;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly IMemoryCache _cache;

//        public JsReportService(
//            ILogger<JsReportService> logger,
//            IJsReportMVCService jsReportMVCService,
//            IServiceProvider serviceProvider,
//            IMemoryCache cache)
//        {
//            _logger = logger;
//            _jsReportMVCService = jsReportMVCService;
//            _serviceProvider = serviceProvider;
//            _cache = cache;
//        }

//        // ╔════════════════════════════════════════════════════════════════════╗
//        // ║ CACHE HELPERS                                                      ║
//        // ╚════════════════════════════════════════════════════════════════════╝

//        public bool TryGetCachedHtml(string reportKey)
//            => _cache.TryGetValue(reportKey, out _);

//        public string? GetCachedHtml(string reportKey)
//            => _cache.TryGetValue(reportKey, out string? html) ? html : null;

//        // ╔════════════════════════════════════════════════════════════════════╗
//        // ║ PDF PAGE COUNT                                                     ║
//        // ╚════════════════════════════════════════════════════════════════════╝

//        /// <summary>
//        /// Count pages in a rendered PDF byte array using PdfSharpCore.
//        /// Called after ExportReportToFormatAsync returns PDF bytes.
//        /// Cost: ~1ms — runs entirely in memory, no file I/O.
//        /// </summary>
//        //public static int CountPdfPages(byte[] pdfBytes)
//        //{
//        //    try
//        //    {
//        //        using var ms = new MemoryStream(pdfBytes);
//        //        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.InformationOnly);
//        //        return doc.PageCount;
//        //    }
//        //    catch
//        //    {
//        //        // If PDF is malformed or unreadable, fall back to 1
//        //        return 1;
//        //    }
//        //}


//        public static int CountPdfPages(byte[] pdfBytes)
//        {
//            try
//            {
//                using var ms = new MemoryStream(pdfBytes);
//                using var reader = new iText.Kernel.Pdf.PdfReader(ms);
//                using var doc = new PdfDocument(reader);
//                return doc.GetNumberOfPages();
//            }
//            catch
//            {
//                return 1;
//            }
//        }


//        // ╔════════════════════════════════════════════════════════════════════╗
//        // ║ RENDER PATH — Render Razor → HTML → Cache                         ║
//        // ╚════════════════════════════════════════════════════════════════════╝

//        /// <summary>
//        /// Render Razor view to HTML and cache for later exports
//        /// Called once per report with DB query
//        /// </summary>
//        public async Task<string> RenderRazorToHtmlAndCacheAsync(
//            string reportKey,
//            string reportPath,
//            object data)
//        {
//            try
//            {
//                // ✅ Return cached if already rendered
//                if (_cache.TryGetValue(reportKey, out string? cachedHtml))
//                {
//                    return cachedHtml!;
//                }

//                // ✅ Render Razor .cshtml to HTML string
//                var html = await RenderRazorViewToStringAsync(reportPath, data);

//                // ✅ Cache with sliding + absolute expiration
//                _cache.Set(reportKey, html, new MemoryCacheEntryOptions()
//                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))
//                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(20)));
//                ;
//                return html;
//            }
//            catch (Exception ex)
//            {
//                throw new Exception($"Render and cache failed: {ex.Message}", ex);
//            }
//        }

//        // ╔════════════════════════════════════════════════════════════════════╗
//        // ║ EXPORT PATH — Convert cached HTML to PDF/Excel/PNG                ║
//        // ║ Called MANY times (NO DB calls — uses cache)                       ║
//        // ╚════════════════════════════════════════════════════════════════════╝

//        /// <summary>
//        /// Convert HTML to any supported format (PDF, Excel, PNG, HTML)
//        /// Always called with cached HTML — NO DB CALLS
//        /// </summary>
//        public async Task<byte[]> ExportReportToFormatAsync(
//            string htmlContent,
//            string format,
//            string? reportKey = null,
//            PageSizeSetting? pageSetting = null)
//        {
//            try
//            {
//                var upperFormat = format.ToUpper();
//                var isPdf = upperFormat == "PDF" || upperFormat == "VIEW";

//                // ✅ Check cache for compressed PDF (if reportKey provided)
//                if (reportKey != null && isPdf)
//                {
//                    var cacheKey = $"{reportKey}_{upperFormat}_compressed";
//                    if (_cache.TryGetValue(cacheKey, out byte[] cachedPdf))
//                    {
//                        _logger.LogInformation("📦 Cache hit – returning compressed PDF for {CacheKey}", cacheKey);
//                        return cachedPdf;
//                    }
//                }

//                // ✅ Create jsreport render request
//                var renderRequest = new RenderRequest
//                {
//                    Template = new Template
//                    {
//                        Content = htmlContent,
//                        Engine = Engine.None,
//                        Recipe = GetRecipe(upperFormat)
//                    },
//                    Options = new RenderOptions
//                    {
//                        Timeout = 600000  // 10 minutes in milliseconds
//                    }
//                };

//                ConfigureJsReportTemplate(renderRequest, upperFormat, pageSetting);
//                var result = await _jsReportMVCService.RenderAsync(renderRequest);

//                using var ms = new MemoryStream();
//                await result.Content.CopyToAsync(ms);
//                var rawBytes = ms.ToArray();

//                // 3️⃣ Compress if PDF, otherwise return raw
//                byte[] finalBytes = isPdf
//                    ? ReportUtils.CompressPdf(rawBytes, _logger)
//                    : rawBytes;

//                _logger.LogInformation("✅ Exported {Format}: {Bytes} bytes (compressed)",
//                    upperFormat, finalBytes.Length);

//                // 4️⃣ Store compressed PDF in cache (if reportKey provided)
//                if (reportKey != null && isPdf)
//                {
//                    var cacheKey = $"{reportKey}_{upperFormat}_compressed";
//                    _cache.Set(cacheKey, finalBytes, new MemoryCacheEntryOptions()
//                        .SetSlidingExpiration(TimeSpan.FromMinutes(10))
//                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(20)));
//                }
//                return finalBytes;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ Export error ({Format}): {Msg}", format.ToUpper(), ex.Message);
//                throw new Exception($"Export to {format} failed: {ex.Message}", ex);
//            }
//        }

//        // ╔════════════════════════════════════════════════════════════════════╗
//        // ║ JSREPORT ENGINE — HTML → PDF/Excel/PNG conversions                ║
//        // ╚════════════════════════════════════════════════════════════════════╝

//        /// <summary>
//        /// Map export format string to jsreport recipe enum
//        /// </summary>
//        private static Recipe GetRecipe(string format) => format switch
//        {
//            "PDF" or "VIEW" => Recipe.ChromePdf,
//            "HTML" => Recipe.Html,
//            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
//            "WORD" or "DOCX" => Recipe.HtmlEmbeddedInDocx,
//            "PNG" => Recipe.ChromeImage,
//            _ => Recipe.ChromePdf
//        };

//        /// <summary>
//        /// Apply format-specific options (margins, page size, etc.) to jsreport request
//        /// </summary>
//        private static void ConfigureJsReportTemplate(RenderRequest request, string format, PageSizeSetting? pageSetting = null)
//        {
//            switch (format)
//            {
//                case "PDF":
//                case "VIEW":
//                    var opts = pageSetting ?? new PageSizeSetting();

//                    var chrome = new Chrome
//                    {
//                        MarginTop = opts.MarginTop,
//                        MarginBottom = opts.MarginBottom,
//                        MarginLeft = opts.MarginLeft,
//                        MarginRight = opts.MarginRight,
//                        WaitForJS = false,
//                        DisplayHeaderFooter = true,
//                        PrintBackground = false,
//                        Landscape = opts.Landscape,

//                        // ── Footer template with page numbering ──────────────────
//                        HeaderTemplate = "<span></span>",
//                        FooterTemplate = @"
//                        <div style='
//                                font-size: 5pt;
//                                width: 100%; 
//                                text-align: center;
//                                border-top: 1px solid #ccc;
//                                line-height: 20px;
//                                padding:0; margin: 0;'>
//                               <span class='pageNumber'></span> of <span class='totalPages'></span>
//                         </div>
//                        ",
//                    };

//                    if (opts.ResolvedFormat != null)
//                        chrome.Format = opts.ResolvedFormat;      // named format: A4, A3…
//                    else
//                    {
//                        chrome.Width = opts.ResolvedWidth;       // custom: "380mm"
//                        chrome.Height = opts.ResolvedHeight;      // custom: "210mm"
//                    }

//                    request.Template.Chrome = chrome;



//                    //request.Template.Chrome = new Chrome
//                    //{
//                    //    MarginTop = "1mm",
//                    //    MarginBottom = "1mm",
//                    //    MarginLeft = "5mm",
//                    //    MarginRight = "5mm",
//                    //    DisplayHeaderFooter = false,
//                    //    PrintBackground = true,
//                    //    Format = "A4",
//                    //    Landscape = false
//                    //};
//                    break;

//                case "EXCEL":
//                case "XLSX":
//                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
//                    break;

//            }
//        }

//        // ╔════════════════════════════════════════════════════════════════════╗
//        // ║ INTERNAL HELPERS — Razor rendering                                ║
//        // ╚════════════════════════════════════════════════════════════════════╝

//        /// <summary>
//        /// Render Razor .cshtml file to HTML string
//        /// </summary>
//        private async Task<string> RenderRazorViewToStringAsync(string viewName, object model)
//        {
//            try
//            {
//                // ✅ Create scope to avoid captive dependency issues
//                using var scope = _serviceProvider.CreateScope();
//                var scopedProvider = scope.ServiceProvider;

//                var httpContext = new DefaultHttpContext { RequestServices = scopedProvider };
//                var actionContext = new ActionContext(
//                    httpContext,
//                    new RouteData(),
//                    new ActionDescriptor());

//                var viewEngine = scopedProvider.GetRequiredService<IRazorViewEngine>();
//                var tempDataProvider = scopedProvider.GetRequiredService<ITempDataProvider>();

//                // ✅ Try absolute path first, then by name
//                var viewResult = viewEngine.GetView(null, viewName, false);
//                if (!viewResult.Success)
//                    viewResult = viewEngine.FindView(actionContext, viewName, false);

//                if (!viewResult.Success)
//                    throw new InvalidOperationException($"View '{viewName}' not found.");

//                await using var sw = new StringWriter();

//                var viewData = new ViewDataDictionary(
//                    new EmptyModelMetadataProvider(),
//                    new ModelStateDictionary())
//                { Model = model };

//                var viewContext = new ViewContext(
//                    actionContext,
//                    viewResult.View,
//                    viewData,
//                    new TempDataDictionary(httpContext, tempDataProvider),
//                    sw,
//                    new HtmlHelperOptions());

//                await viewResult.View.RenderAsync(viewContext);
//                return sw.ToString();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "❌ RenderRazorViewToStringAsync failed: {Msg}", ex.Message);
//                throw;
//            }
//        }
//    }
//}










//using iText.Kernel.Font;
//using iText.Kernel.Pdf;
//using iText.Kernel.Pdf.Canvas;
//using jsreport.AspNetCore;
//using jsreport.Types;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.Abstractions;
//using Microsoft.AspNetCore.Mvc.ModelBinding;
//using Microsoft.AspNetCore.Mvc.Razor;
//using Microsoft.AspNetCore.Mvc.Rendering;
//using Microsoft.AspNetCore.Mvc.ViewFeatures;
//using Microsoft.Extensions.Caching.Memory;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Utils.Report;

//namespace NexgenCosysReport.Services.ReportService
//{
//    public class JsReportService : IJsReportService
//    {
//        private readonly ILogger<JsReportService> _logger;
//        private readonly IJsReportMVCService _jsReport;
//        private readonly IServiceProvider _serviceProvider;
//        private readonly IMemoryCache _cache;

//        // ── Constants ──────────────────────────────────────────────────────
//        private static readonly TimeSpan CacheSliding = TimeSpan.FromMinutes(30);
//        private static readonly TimeSpan CacheAbsolute = TimeSpan.FromMinutes(60);
//        private const int RenderTimeoutMs = 600_000;

//        private const string FooterHtml = """
//            <div style='font-size:5pt;width:100%;text-align:center;border-top:1px solid #ccc;line-height:20px;'>
//                <span class='pageNumber'></span> of <span class='totalPages'></span>
//            </div>
//            """;

//        public JsReportService(
//            ILogger<JsReportService> logger,
//            IJsReportMVCService jsReport,
//            IServiceProvider serviceProvider,
//            IMemoryCache cache)
//        {
//            _logger = logger;
//            _jsReport = jsReport;
//            _serviceProvider = serviceProvider;
//            _cache = cache;
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // CACHE HELPERS
//        // ═══════════════════════════════════════════════════════════════════

//        public bool TryGetCachedHtml(string reportKey, out string? html)
//            => _cache.TryGetValue(reportKey, out html);

//        public bool TryGetCachedPdf(string reportKey, out byte[]? pdf)
//            => _cache.TryGetValue(PdfCacheKey(reportKey), out pdf);

//        private static string PdfCacheKey(string reportKey) => $"{reportKey}_PDF_compressed";

//        private void CacheHtml(string key, string html)
//            => _cache.Set(key, html, BuildCacheOptions());

//        private void CachePdf(string key, byte[] pdf)
//            => _cache.Set(PdfCacheKey(key), pdf, BuildCacheOptions());

//        private static MemoryCacheEntryOptions BuildCacheOptions() => new MemoryCacheEntryOptions()
//            .SetSlidingExpiration(CacheSliding)
//            .SetAbsoluteExpiration(CacheAbsolute);
//        // Cache PDFs only if smaller than this — prevents IMemoryCache bloat
//        private const long PdfCacheMaxBytes = 50L * 1024 * 1024;

//        private static string ChunkedPdfPathCacheKey(string reportKey) => $"{reportKey}_chunked_path";

//        public bool TryGetChunkedPdfPath(string reportKey, out string? pdfPath)
//            => _cache.TryGetValue(ChunkedPdfPathCacheKey(reportKey), out pdfPath);

//        private void CacheChunkedPdfPath(string reportKey, string path)
//        {
//            var options = new MemoryCacheEntryOptions
//            {
//                SlidingExpiration = TimeSpan.FromMinutes(30),
//                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
//            };

//            // ← NEW: clean up the temp file when the cache entry expires
//            options.RegisterPostEvictionCallback((_, value, _, _) =>
//            {
//                if (value is string filePath)
//                {
//                    TryDelete(filePath);
//                    // Also delete the parent temp dir if it's now empty
//                    try
//                    {
//                        var dir = Path.GetDirectoryName(filePath);
//                        if (dir != null && Directory.Exists(dir) &&
//                            !Directory.EnumerateFileSystemEntries(dir).Any())
//                            Directory.Delete(dir);
//                    }
//                    catch { /* best-effort */ }
//                }
//            });

//            _cache.Set(ChunkedPdfPathCacheKey(reportKey), path, options);
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // PDF PAGE COUNT
//        // ═══════════════════════════════════════════════════════════════════

//        public static int CountPdfPages(byte[] pdfBytes)
//        {
//            try
//            {
//                using var ms = new MemoryStream(pdfBytes);
//                using var reader = new PdfReader(ms);
//                using var doc = new PdfDocument(reader);
//                return doc.GetNumberOfPages();
//            }
//            catch { return 1; }
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // RENDER: Razor → HTML → Cache
//        // ═══════════════════════════════════════════════════════════════════

//        public async Task<string> RenderRazorToHtmlAndCacheAsync(
//            string reportKey, string reportPath, object data, CancellationToken ct = default)
//        {
//            if (TryGetCachedHtml(reportKey, out var cached) && cached != null)
//                return cached;

//            var html = await RenderRazorViewToStringAsync(reportPath, data).ConfigureAwait(false);
//            CacheHtml(reportKey, html);
//            return html;
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // EXPORT: HTML → PDF / Excel / PNG
//        // ═══════════════════════════════════════════════════════════════════

//        public async Task<byte[]> ExportReportToFormatAsync(
//            string htmlContent,
//            string format,
//            string? reportKey = null,
//            PageSizeSetting? pageSetting = null,
//            CancellationToken ct = default)
//        {
//            var fmt = format.ToUpperInvariant();
//            var isPdf = fmt is "PDF" or "VIEW";

//            // ── Cache hit ──────────────────────────────────────────────────
//            if (isPdf && reportKey != null && TryGetCachedPdf(reportKey, out var cachedPdf) && cachedPdf != null)
//            {
//                _logger.LogInformation("📦 PDF cache hit — {Key}", reportKey);
//                return cachedPdf;
//            }

//            // ── Render ─────────────────────────────────────────────────────
//            var rawBytes = await RenderHtmlToBytesAsync(htmlContent, fmt, pageSetting, ct)
//                                .ConfigureAwait(false);

//            var finalBytes = isPdf ? ReportUtils.CompressPdf(rawBytes, _logger) : rawBytes;

//            _logger.LogInformation("✅ Exported {Format}: {MB:F2} MB",
//                fmt, finalBytes.Length / 1024.0 / 1024.0);

//            if (isPdf && reportKey != null)
//                CachePdf(reportKey, finalBytes);

//            return finalBytes;
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // CHUNKED PDF: parallel rendering, merge, compress
//        // ═══════════════════════════════════════════════════════════════════



//        // 50 MB

//        public async Task<string> ExportChunkedPdfAsync(
//            string reportKey,
//            string reportPath,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            int chunkSize,
//            PageSizeSetting? pageSetting = null,
//            int maxParallelism = 2,
//            CancellationToken ct = default)
//        {
//            if (!reportData.TryGetValue(rowsKey, out var rowsObj) ||
//                rowsObj is not IEnumerable<object> rowsEnum)
//                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

//            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
//            var chunks = SplitIntoChunks(allRows, chunkSize);

//            _logger.LogInformation("🔀 {Total} rows → {Count} chunks × {Size} (parallel={P})",
//                allRows.Count, chunks.Count, chunkSize, maxParallelism);

//            // ── Temp directory for this report ─────────────────────────────
//            var tempDir = Path.Combine(Path.GetTempPath(), "nexgen_pdf", Guid.NewGuid().ToString("N"));
//            Directory.CreateDirectory(tempDir);

//            var chunkFiles = new string[chunks.Count];

//            try
//            {
//                // ── Render chunks to disk (bounded parallelism) ────────────
//                using var sem = new SemaphoreSlim(maxParallelism);
//                var tasks = new List<Task>(chunks.Count);

//                for (int i = 0; i < chunks.Count; i++)
//                {
//                    int idx = i;
//                    await sem.WaitAsync(ct).ConfigureAwait(false);

//                    tasks.Add(Task.Run(async () =>
//                    {
//                        try
//                        {
//                            // Build per-chunk model — share other keys, replace rows
//                            var chunkData = new Dictionary<string, object>(reportData)
//                            {
//                                [rowsKey] = chunks[idx]
//                            };

//                            var html = await RenderRazorViewToStringAsync(reportPath, chunkData)
//                                .ConfigureAwait(false);

//                            var chunkFile = Path.Combine(tempDir, $"chunk_{idx:D5}.pdf");

//                            // ✅ Stream jsreport response directly to disk — no byte[] in memory
//                            await RenderHtmlToFileAsync(html, "PDF", pageSetting, chunkFile, ct)
//                                .ConfigureAwait(false);

//                            chunkFiles[idx] = chunkFile;

//                            // Free chunk row references ASAP
//                            chunks[idx] = null!;

//                            var fi = new FileInfo(chunkFile);
//                            _logger.LogInformation("✅ Chunk {Idx}/{Total} → {MB:F2} MB",
//                                idx + 1, chunks.Count, fi.Length / 1024.0 / 1024.0);
//                        }
//                        finally { sem.Release(); }
//                    }, ct));
//                }

//                await Task.WhenAll(tasks).ConfigureAwait(false);

//                // Allow GC to reclaim chunk lists before merge
//                chunks.Clear();
//                GC.Collect();
//                GC.WaitForPendingFinalizers();

//                // ── Merge files → single output PDF (streamed via iText) ───
//                var mergedFile = Path.Combine(tempDir, "merged.pdf");
//                MergePdfFiles(chunkFiles, mergedFile);

//                // ── Stamp correct page numbers ───────────────────────
//                StampPageNumbers(mergedFile);
//                // After stamping and before returning
//                CacheChunkedPdfPath(reportKey, mergedFile);
//                var mergedSize = new FileInfo(mergedFile).Length;
//                _logger.LogInformation("🔗 Merged PDF: {MB:F2} MB", mergedSize / 1024.0 / 1024.0);

//                // ── Optional: cache only if small enough ───────────────────
//                if (mergedSize <= PdfCacheMaxBytes)
//                {
//                    var bytes = await File.ReadAllBytesAsync(mergedFile, ct).ConfigureAwait(false);
//                    CachePdf(reportKey, bytes);
//                    _logger.LogInformation("📦 Cached PDF ({MB:F2} MB)", bytes.Length / 1024.0 / 1024.0);
//                }
//                else
//                {
//                    _logger.LogInformation("⏭️ Skipping cache — PDF too large ({MB:F2} MB)",
//                        mergedSize / 1024.0 / 1024.0);
//                }

//                // Delete chunk files; caller handles merged file lifetime
//                foreach (var f in chunkFiles)
//                    TryDelete(f);

//                return mergedFile;
//            }
//            catch
//            {
//                // Cleanup on failure
//                try { Directory.Delete(tempDir, recursive: true); } catch { }
//                throw;
//            }
//        }

//        private static void StampPageNumbers(string inputPdfPath)
//        {
//            // Work on a temp file to avoid locking conflicts
//            var tempPath = Path.Combine(Path.GetDirectoryName(inputPdfPath) ?? ".",
//                                         Path.GetFileNameWithoutExtension(inputPdfPath) + "_stamped.pdf");

//            using (var reader = new PdfReader(inputPdfPath))
//            using (var writer = new PdfWriter(tempPath))
//            using (var doc = new PdfDocument(reader, writer))
//            {
//                int totalPages = doc.GetNumberOfPages();
//                var font = PdfFontFactory.CreateFont();

//                for (int i = 1; i <= totalPages; i++)
//                {
//                    var page = doc.GetPage(i);
//                    var rect = page.GetPageSize();
//                    var canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), doc);

//                    float x = (rect.GetLeft() + rect.GetRight()) / 2f;
//                    float y = rect.GetBottom() + 15;
//                    canvas.BeginText()
//                          .SetFontAndSize(font, 5)
//                          .MoveText(x, y)
//                          .ShowText($"{i} of {totalPages}")
//                          .EndText();
//                }
//            }

//            // Replace original with stamped version
//            File.Delete(inputPdfPath);
//            File.Move(tempPath, inputPdfPath);
//        }

//        // ─── Stream jsreport output directly to disk ───────────────────────
//        private async Task RenderHtmlToFileAsync(
//            string html, string format, PageSizeSetting? pageSetting,
//            string outputPath, CancellationToken ct)
//        {
//            var request = new RenderRequest
//            {
//                Template = new Template
//                {
//                    Content = html,
//                    Engine = Engine.None,
//                    Recipe = GetRecipe(format)
//                },
//                Options = new RenderOptions { Timeout = RenderTimeoutMs }
//            };

//            ConfigureJsReportTemplate(request, format, pageSetting, suppressFooter: true);

//            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);

//            // ✅ FileStream with large buffer — never materializes full PDF in memory
//            await using var fs = new FileStream(
//                outputPath, FileMode.Create, FileAccess.Write, FileShare.None,
//                bufferSize: 81920, useAsync: true);

//            await result.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
//        }

//        // ─── Merge PDFs from disk (iText streams chunk by chunk) ───────────
//        private static void MergePdfFiles(string[] inputFiles, string outputFile)
//        {
//            using var outFs = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
//            using var writer = new PdfWriter(outFs);
//            using var outDoc = new PdfDocument(writer);
//            var merger = new iText.Kernel.Utils.PdfMerger(outDoc);

//            foreach (var file in inputFiles)
//            {
//                if (string.IsNullOrEmpty(file) || !File.Exists(file)) continue;
//                using var inFs = new FileStream(file, FileMode.Open, FileAccess.Read);
//                using var reader = new PdfReader(inFs);
//                using var inDoc = new PdfDocument(reader);
//                merger.Merge(inDoc, 1, inDoc.GetNumberOfPages());
//            }
//            // ✅ Explicit close flushes iText's internal buffers to the OS
//            // before the using-var disposals unwind. PdfDocument.Close() is
//            // idempotent — the subsequent implicit Dispose() is a no-op.
//            outDoc.Close();
//        }

//        private static void TryDelete(string? path)
//        {
//            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
//            catch { /* ignore */ }
//        }


//        // ═══════════════════════════════════════════════════════════════════
//        // SHARED: HTML → bytes (single render entry point)
//        // ═══════════════════════════════════════════════════════════════════

//        private async Task<byte[]> RenderHtmlToBytesAsync(
//            string html, string format, PageSizeSetting? pageSetting, CancellationToken ct, bool suppressFooter = false)
//        {
//            var request = new RenderRequest
//            {
//                Template = new Template
//                {
//                    Content = html,
//                    Engine = Engine.None,
//                    Recipe = GetRecipe(format)
//                },
//                Options = new RenderOptions { Timeout = RenderTimeoutMs }
//            };

//            ConfigureJsReportTemplate(request, format, pageSetting, suppressFooter);

//            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);
//            using var ms = new MemoryStream();
//            await result.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
//            return ms.ToArray();
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // JSREPORT CONFIG
//        // ═══════════════════════════════════════════════════════════════════

//        private static Recipe GetRecipe(string format) => format switch
//        {
//            "PDF" or "VIEW" => Recipe.ChromePdf,
//            "HTML" => Recipe.Html,
//            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
//            "WORD" or "DOCX" => Recipe.HtmlEmbeddedInDocx,
//            "PNG" => Recipe.ChromeImage,
//            _ => Recipe.ChromePdf
//        };

//        private static void ConfigureJsReportTemplate(
//            RenderRequest request, string format, PageSizeSetting? pageSetting, bool suppressFooter = false)
//        {
//            switch (format)
//            {
//                case "PDF":
//                case "VIEW":
//                    var opts = pageSetting ?? new PageSizeSetting();
//                    var chrome = new Chrome
//                    {
//                        MarginTop = opts.MarginTop,
//                        MarginBottom = opts.MarginBottom,
//                        MarginLeft = opts.MarginLeft,
//                        MarginRight = opts.MarginRight,
//                        WaitForJS = false,
//                        WaitForNetworkIddle = true,
//                        DisplayHeaderFooter = !suppressFooter,
//                        PrintBackground = false,
//                        Landscape = opts.Landscape,
//                        HeaderTemplate = suppressFooter ? null : "<span></span>",
//                        FooterTemplate = suppressFooter ? null : FooterHtml
//                    };

//                    if (opts.ResolvedFormat != null)
//                        chrome.Format = opts.ResolvedFormat;
//                    else
//                    {
//                        chrome.Width = opts.ResolvedWidth;
//                        chrome.Height = opts.ResolvedHeight;
//                    }
//                    request.Template.Chrome = chrome;
//                    break;

//                case "EXCEL":
//                case "XLSX":
//                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
//                    break;
//            }
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // RAZOR → STRING
//        // ═══════════════════════════════════════════════════════════════════

//        private async Task<string> RenderRazorViewToStringAsync(string viewName, object model)
//        {
//            using var scope = _serviceProvider.CreateScope();
//            var sp = scope.ServiceProvider;
//            var httpContext = new DefaultHttpContext { RequestServices = sp };
//            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
//            var viewEngine = sp.GetRequiredService<IRazorViewEngine>();
//            var tempProvider = sp.GetRequiredService<ITempDataProvider>();

//            var viewResult = viewEngine.GetView(null, viewName, false);
//            if (!viewResult.Success)
//                viewResult = viewEngine.FindView(actionContext, viewName, false);
//            if (!viewResult.Success)
//                throw new InvalidOperationException($"View '{viewName}' not found.");

//            await using var sw = new StringWriter();
//            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
//            {
//                Model = model
//            };

//            var viewContext = new ViewContext(
//                actionContext, viewResult.View, viewData,
//                new TempDataDictionary(httpContext, tempProvider),
//                sw, new HtmlHelperOptions());

//            await viewResult.View.RenderAsync(viewContext).ConfigureAwait(false);
//            return sw.ToString();
//        }

//        // ═══════════════════════════════════════════════════════════════════
//        // CHUNKING (O(n) — no Skip/Take)
//        // ═══════════════════════════════════════════════════════════════════

//        private static List<IList<object>> SplitIntoChunks(IList<object> source, int chunkSize)
//        {
//            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

//            var total = source.Count;
//            var count = (total + chunkSize - 1) / chunkSize;
//            var chunks = new List<IList<object>>(count);

//            for (int start = 0; start < total; start += chunkSize)
//            {
//                var size = Math.Min(chunkSize, total - start);
//                var chunk = new List<object>(size);
//                for (int i = 0; i < size; i++)
//                    chunk.Add(source[start + i]);
//                chunks.Add(chunk);
//            }
//            return chunks;
//        }


//    }
//}











using iText.Kernel.Pdf;
using jsreport.AspNetCore;
using jsreport.Types;
using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Services.ReportService
{
    public class JsReportService : IJsReportService
    {
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReport;
        private readonly IRazorRenderService _razorRenderer;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheSliding = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan CacheAbsolute = TimeSpan.FromMinutes(60);
        private const int RenderTimeoutMs = 600_000;

        public JsReportService(
            ILogger<JsReportService> logger,
            IJsReportMVCService jsReport,
            IRazorRenderService razorRenderer,
            IMemoryCache cache)
        {
            _logger = logger;
            _jsReport = jsReport;
            _razorRenderer = razorRenderer;
            _cache = cache;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CACHE
        // ═══════════════════════════════════════════════════════════════════

        public bool TryGetCachedHtml(string reportKey, out string? html)
            => _cache.TryGetValue(reportKey, out html);

        public bool TryGetCachedPdf(string reportKey, out byte[]? pdf)
            => _cache.TryGetValue(PdfCacheKey(reportKey), out pdf);

        private static string PdfCacheKey(string key) => $"{key}_PDF_compressed";

        private void CacheHtml(string key, string html)
            => _cache.Set(key, html, BuildCacheOptions());

        private void CachePdf(string key, byte[] pdf)
            => _cache.Set(PdfCacheKey(key), pdf, BuildCacheOptions());

        private static MemoryCacheEntryOptions BuildCacheOptions() =>
            new MemoryCacheEntryOptions()
                .SetSlidingExpiration(CacheSliding)
                .SetAbsoluteExpiration(CacheAbsolute);

        // ═══════════════════════════════════════════════════════════════════
        // PDF PAGE COUNT (static — callable without DI)
        // ═══════════════════════════════════════════════════════════════════

        public static int CountPdfPages(byte[] pdfBytes)
        {
            try
            {
                using var ms = new MemoryStream(pdfBytes);
                using var reader = new PdfReader(ms);
                using var doc = new PdfDocument(reader);
                return doc.GetNumberOfPages();
            }
            catch { return 1; }
        }

        // ═══════════════════════════════════════════════════════════════════
        // RENDER: Razor → HTML → Cache
        // ═══════════════════════════════════════════════════════════════════

        public async Task<string> RenderRazorToHtmlAndCacheAsync(
            string reportKey, string reportPath, object data, CancellationToken ct = default)
        {
            if (TryGetCachedHtml(reportKey, out var cached) && cached != null)
                return cached;

            var html = await _razorRenderer.RenderToStringAsync(reportPath, data).ConfigureAwait(false);
            CacheHtml(reportKey, html);
            return html;
        }

        // ═══════════════════════════════════════════════════════════════════
        // EXPORT: HTML → PDF / Excel / PNG bytes
        // ═══════════════════════════════════════════════════════════════════

        public async Task<byte[]> ExportReportToFormatAsync(
            string htmlContent,
            string format,
            string? reportKey = null,
            PageSizeSetting? pageSetting = null,
            CancellationToken ct = default)
        {
            var fmt = format.ToUpperInvariant();
            var isPdf = fmt is "PDF" or "VIEW";

            // Cache hit
            if (isPdf && reportKey != null &&
                TryGetCachedPdf(reportKey, out var cached) && cached != null)
            {
                _logger.LogInformation("📦 PDF cache hit — {Key}", reportKey);
                return cached;
            }

            var rawBytes = await RenderHtmlToBytesAsync(htmlContent, fmt, pageSetting, ct).ConfigureAwait(false);
            var finalBytes = isPdf ? ReportUtils.CompressPdf(rawBytes, _logger) : rawBytes;

            _logger.LogInformation("✅ Exported {Format}: {MB:F2} MB",
                fmt, finalBytes.Length / 1024.0 / 1024.0);

            if (isPdf && reportKey != null)
                CachePdf(reportKey, finalBytes);

            return finalBytes;
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE
        // ═══════════════════════════════════════════════════════════════════

        private async Task<byte[]> RenderHtmlToBytesAsync(
            string html, string format, PageSizeSetting? pageSetting, CancellationToken ct)
        {
            var request = new RenderRequest
            {
                Template = new Template
                {
                    Content = html,
                    Engine = Engine.None,
                    Recipe = JsReportTemplateHelper.GetRecipe(format)
                },
                Options = new RenderOptions { Timeout = RenderTimeoutMs }
            };

            JsReportTemplateHelper.ConfigureTemplate(request, format, pageSetting, suppressFooter: false);

            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);
            using var ms = new MemoryStream();
            await result.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
            return ms.ToArray();
        }
    }
}
