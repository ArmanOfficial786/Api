////working fine for progressive pdf with pagination
//using iText.Kernel.Colors;
//using iText.Kernel.Font;
//using iText.Kernel.Pdf;
//using iText.Kernel.Utils;
//using iText.Layout;
//using iText.Layout.Element;
//using iText.Layout.Properties;
//using jsreport.AspNetCore;
//using jsreport.Types;
//using Microsoft.Extensions.Caching.Memory;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Utils.Report;
//using System.Diagnostics;

//namespace NexgenCosysReport.Services.ReportService
//{
//    public class ProgressivePdfService : IProgressivePdfService
//    {
//        private readonly IRazorRenderService _razor;
//        private readonly IJsReportMVCService _jsReport;
//        private readonly IMemoryCache _cache;
//        private readonly ILogger<ProgressivePdfService> _logger;
//        private const int RenderTimeoutMs = 600_000;

//        public ProgressivePdfService(
//            IRazorRenderService razor,
//            IJsReportMVCService jsReport,
//            IMemoryCache cache,
//            ILogger<ProgressivePdfService> logger)
//        {
//            _razor = razor;
//            _jsReport = jsReport;
//            _cache = cache;
//            _logger = logger;
//        }

//        // ───────────────────────────────────────────────────────────────────
//        // PUBLIC API
//        // ───────────────────────────────────────────────────────────────────

//        public async Task<ProgressivePdfJob> StartAsync(
//            string reportPath,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            int firstChunkSize,
//            int subsequentChunkSize,
//            PageSizeSetting? pageSetting,
//            CancellationToken ct)
//        {
//            if (!reportData.TryGetValue(rowsKey, out var rowsObj) ||
//                rowsObj is not IEnumerable<object> rowsEnum)
//                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

//            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
//            var chunks = BuildChunks(allRows, firstChunkSize, subsequentChunkSize);

//            var tempDir = Path.Combine(
//                Path.GetTempPath(), "nexgen_progressive", Guid.NewGuid().ToString("N"));
//            Directory.CreateDirectory(tempDir);

//            var job = new ProgressivePdfJob
//            {
//                LivePdfPath = Path.Combine(tempDir, "live.pdf"),
//                TempDir = tempDir,
//                TotalChunks = chunks.Count,
//                EstimatedTotalPages = EstimateTotalPages(allRows.Count, firstChunkSize, subsequentChunkSize)
//            };

//            _cache.Set(CacheKey(job.JobId), job, new MemoryCacheEntryOptions
//            {
//                SlidingExpiration = TimeSpan.FromMinutes(30),
//                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
//            }.RegisterPostEvictionCallback((_, val, _, _) =>
//            {
//                if (val is ProgressivePdfJob j) TryCleanup(j.TempDir);
//            }));

//            _logger.LogInformation(
//                "🚀 Job {Id} started – {Rows} rows → {Chunks} chunks (first={F}, rest={R})",
//                job.JobId, allRows.Count, chunks.Count, firstChunkSize, subsequentChunkSize);

//            await RenderAndAppendChunkAsync(
//                job, reportData, rowsKey, chunks[0], 0, reportPath, pageSetting, ct)
//                    .ConfigureAwait(false);

//            if (chunks.Count > 1)
//            {
//                _ = Task.Run(async () =>
//                {
//                    try
//                    {
//                        for (int i = 1; i < chunks.Count; i++)
//                        {
//                            await RenderAndAppendChunkAsync(
//                                job, reportData, rowsKey, chunks[i], i,
//                                reportPath, pageSetting, CancellationToken.None)
//                                    .ConfigureAwait(false);
//                        }
//                        _logger.LogInformation(
//                            "✔️ Job {Id} complete — {Pages} pages", job.JobId, job.PagesReady);
//                    }
//                    catch (Exception ex)
//                    {
//                        job.HasError = true;
//                        job.ErrorMessage = ex.Message;
//                        _logger.LogError(ex, "❌ Job {Id} background render failed", job.JobId);
//                    }
//                });
//            }

//            return job;
//        }

//        public ProgressivePdfJob? GetJob(string jobId)
//            => _cache.TryGetValue(CacheKey(jobId), out ProgressivePdfJob? job) ? job : null;

//        // ───────────────────────────────────────────────────────────────────
//        // CORE
//        // ───────────────────────────────────────────────────────────────────

//        private async Task RenderAndAppendChunkAsync(
//            ProgressivePdfJob job,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            IList<object> chunkRows,
//            int chunkIndex,
//            string reportPath,
//            PageSizeSetting? pageSetting,
//            CancellationToken ct)
//        {
//            var sw = Stopwatch.StartNew();

//            // 1. Razor → HTML
//            var chunkData = new Dictionary<string, object>(reportData)
//            {
//                [rowsKey] = chunkRows,
//                ["ShowHeader"] = chunkIndex == 0   // ✅ only first chunk shows header
//            };
//            var html = await _razor.RenderToStringAsync(reportPath, chunkData).ConfigureAwait(false);

//            // 2. HTML → chunk PDF
//            //    suppressFooter: true — jsreport must NOT add per-chunk page numbers.
//            //    We stamp globally-correct "N of Total" with iText after every merge.
//            var chunkPdf = Path.Combine(job.TempDir, $"chunk_{chunkIndex:D5}.pdf");
//            await RenderHtmlToFileAsync(html, pageSetting, chunkPdf, ct).ConfigureAwait(false);

//            await job.FileLock.WaitAsync(ct).ConfigureAwait(false);
//            try
//            {
//                // base.pdf  → unstamped merged accumulator (never stamped, always clean)
//                // live.pdf  → base.pdf + fresh "N of Total" stamps (client download)
//                //
//                // Because base.pdf is never stamped, StampBasePdfToLive always starts
//                // from a clean slate — no old stamp removal ever needed, zero overlap.
//                var basePdf = Path.Combine(job.TempDir, "base.pdf");

//                if (chunkIndex == 0)
//                    File.Copy(chunkPdf, basePdf, overwrite: true);
//                else
//                    AppendPdf(basePdf, chunkPdf);

//                StampBasePdfToLive(basePdf, job.LivePdfPath, out int pageCount);

//                job.PagesReady = pageCount;
//                job.CompletedChunks = chunkIndex + 1;
//                job.CurrentSizeBytes = new FileInfo(job.LivePdfPath).Length;
//                job.LastUpdated = DateTime.UtcNow;
//            }
//            finally
//            {
//                job.FileLock.Release();
//            }

//            TryDelete(chunkPdf);

//            _logger.LogInformation(
//                "📄 Job {Id} chunk {Idx}/{Tot} → {Pages} pages, {MB:F2} MB in {Ms} ms",
//                job.JobId, chunkIndex + 1, job.TotalChunks,
//                job.PagesReady, job.CurrentSizeBytes / 1024.0 / 1024.0,
//                sw.ElapsedMilliseconds);
//        }

//        // ───────────────────────────────────────────────────────────────────
//        // PDF OPERATIONS
//        // ───────────────────────────────────────────────────────────────────

//        private async Task RenderHtmlToFileAsync(
//            string html,
//            PageSizeSetting? pageSetting,
//            string outputPath,
//            CancellationToken ct)
//        {
//            var request = new RenderRequest
//            {
//                Template = new Template
//                {
//                    Content = html,
//                    Engine = Engine.None,
//                    Recipe = JsReportTemplateHelper.GetRecipe("PDF")
//                },
//                Options = new RenderOptions { Timeout = RenderTimeoutMs }
//            };
//            JsReportTemplateHelper.ConfigureTemplate(request, "PDF", pageSetting, suppressFooter: true);

//            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);

//            //await using var fs = new FileStream(
//            //    outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81_920, useAsync: true);
//            //await result.Content.CopyToAsync(fs, ct).ConfigureAwait(false);


//            // 1. Read the raw PDF bytes into a MemoryStream
//            using var ms = new MemoryStream(capacity: 81920);
//            await result.Content.CopyToAsync(ms, ct).ConfigureAwait(false);

//            // 2. Obtain the underlying byte array (avoid extra copy if possible)
//            byte[] rawBytes = ms.TryGetBuffer(out var buffer) ? buffer.Array! : ms.ToArray();

//            // 3. Compress – still returns a valid PDF (assumed)
//            byte[] compressed = ReportUtils.CompressPdf(rawBytes, _logger);

//            // 4. Write the compressed PDF directly to the chunk file
//            await File.WriteAllBytesAsync(outputPath, compressed, ct).ConfigureAwait(false);
//        }

//        /// <summary>
//        /// Appends chunkPath onto livePath using a temp-file swap so iText
//        /// never holds two handles on the same path simultaneously.
//        /// Disposal (reverse of declaration): outDoc → writer → outFs → chunkDoc → liveDoc.
//        /// </summary>
//        private static void AppendPdf(string livePath, string chunkPath)
//        {
//            var tempPath = livePath + ".tmp";
//            {
//                using var liveDoc = new PdfDocument(new PdfReader(livePath));
//                using var chunkDoc = new PdfDocument(new PdfReader(chunkPath));
//                using var outFs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
//                using var writer = new PdfWriter(outFs);
//                using var outDoc = new PdfDocument(writer);

//                var merger = new PdfMerger(outDoc);
//                merger.Merge(liveDoc, 1, liveDoc.GetNumberOfPages());
//                merger.Merge(chunkDoc, 1, chunkDoc.GetNumberOfPages());
//            }
//            File.Delete(livePath);
//            File.Move(tempPath, livePath);
//        }

//        /// <summary>
//        /// Reads the UNSTAMPED <paramref name="basePdfPath"/>, stamps every page
//        /// with centred "N of Total" text in the bottom margin, and writes the
//        /// result to <paramref name="livePdfPath"/> (overwriting any previous live.pdf).
//        ///
//        /// WHY NO OVERLAP IS POSSIBLE:
//        ///   base.pdf is never stamped, so every call starts from a clean file.
//        ///   The old live.pdf is simply overwritten.  No stream removal, no
//        ///   white-rectangle erase, no tagging — nothing to go wrong.
//        ///
//        /// WHY Document.ShowTextAligned INSTEAD OF PdfCanvas:
//        ///   Raw PdfCanvas coordinates map to the PDF user space BEFORE rotation.
//        ///   Chrome/jsreport PDFs can have a /Rotate entry on their pages, which
//        ///   means the visual bottom of the page is not at PDF y=0.  Drawing at
//        ///   y = rect.GetBottom() + 14 then places the text at the visual TOP (or
//        ///   side) of the page, which is the overlap symptom seen in the screenshots.
//        ///
//        ///   Document.ShowTextAligned() works in VISUAL coordinates: it
//        ///   automatically applies the page's rotation transformation so that
//        ///   (pageWidth/2, marginBottom) always means "horizontally centred, inside
//        ///   the bottom margin" regardless of the /Rotate value.  This is the
//        ///   correct, rotation-safe API for absolute text placement in iText 7.
//        /// </summary>
//        private static void StampBasePdfToLive(
//            string basePdfPath,
//            string livePdfPath,
//            out int totalPages)
//        {
//            // Disposal order (reverse of declaration with 'using var'):
//            //   layoutDoc → pdfDoc → writer → reader
//            // layoutDoc must be disposed FIRST so it flushes pending layout
//            // operations into pdfDoc before pdfDoc closes the file.
//            using var reader = new PdfReader(basePdfPath);
//            using var writer = new PdfWriter(livePdfPath);
//            using var pdfDoc = new PdfDocument(reader, writer);
//            using var layoutDoc = new Document(pdfDoc);

//            // Remove all default margins so our absolute coordinates are
//            // relative to the physical page edges, not a content area.
//            layoutDoc.SetMargins(0, 0, 0, 0);

//            totalPages = pdfDoc.GetNumberOfPages();
//            var font = PdfFontFactory.CreateFont();   // Helvetica, no embedding needed

//            for (int i = 1; i <= totalPages; i++)
//            {
//                var page = pdfDoc.GetPage(i);

//                // GetPageSizeWithRotation() returns the VISUAL dimensions of the page
//                // after applying /Rotate — i.e. width = what you see as width in a
//                // PDF viewer, height = what you see as height.
//                var size = page.GetPageSizeWithRotation();

//                // Horizontal centre of the visual page.
//                float x = size.GetWidth() / 2f;

//                // 20 pt up from the visual bottom edge.
//                // Chrome's default bottom margin is ~1 cm ≈ 28 pt, so 20 pt keeps
//                // the text squarely inside the blank bottom margin with space to spare.
//                float y = 20f;

//                string label = $"{i} of {totalPages}";

//                var para = new Paragraph(label)
//                    .SetFont(font)
//                    .SetFontSize(10)
//                    .SetFontColor(ColorConstants.BLACK)
//                    .SetMargin(0)
//                    .SetPadding(0);

//                // ShowTextAligned parameters:
//                //   element   – the Paragraph to render
//                //   x, y      – position in visual (post-rotation) coordinates
//                //   pageNum   – 1-based page number
//                //   textAlign – horizontal alignment relative to x
//                //   vertAlign – vertical alignment relative to y
//                //   radAngle  – rotation of the text itself (0 = horizontal)
//                //
//                // TextAlignment.CENTER   → x is the horizontal centre of the text
//                // VerticalAlignment.BOTTOM → y is the bottom edge of the text baseline
//                layoutDoc.ShowTextAligned(
//                    para,
//                    x, y,
//                    i,
//                    TextAlignment.CENTER,
//                    VerticalAlignment.BOTTOM,
//                    0f);
//            }


//        }

//        // ───────────────────────────────────────────────────────────────────
//        // HELPERS
//        // ───────────────────────────────────────────────────────────────────

//        private static List<IList<object>> BuildChunks(
//            IList<object> rows, int firstSize, int restSize)
//        {
//            var list = new List<IList<object>>();
//            if (rows.Count == 0) return list;

//            int pos = 0;
//            int take = Math.Min(firstSize, rows.Count);
//            list.Add(SliceRows(rows, 0, take));
//            pos += take;

//            while (pos < rows.Count)
//            {
//                take = Math.Min(restSize, rows.Count - pos);
//                list.Add(SliceRows(rows, pos, take));
//                pos += take;
//            }
//            return list;
//        }

//        private static IList<object> SliceRows(IList<object> src, int start, int count)
//        {
//            var slice = new List<object>(count);
//            for (int i = 0; i < count; i++) slice.Add(src[start + i]);
//            return slice;
//        }

//        private static int EstimateTotalPages(int totalRows, int firstSize, int restSize)
//        {
//            const int rowsPerPage = 25;
//            return Math.Max(1, (int)Math.Ceiling((double)totalRows / rowsPerPage));
//        }

//        private static string CacheKey(string id) => $"progressive_pdf_{id}";

//        private static void TryDelete(string path)
//        {
//            try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
//        }

//        private static void TryCleanup(string dir)
//        {
//            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
//        }
//    }
//}


// ProgressivePdfService.cs  —  memory-safe, capped-accumulator, deferred-stamp
//
// ROOT CAUSE OF THE OOM (chunk 13, base=71 MB crash)
// ────────────────────────────────────────────────────
// iText PdfMerger copies content streams verbatim (as raw bytes) regardless of
// the WriterProperties compression level.  The compression level only applies to
// NEW objects iText itself creates (e.g. cross-reference streams, new font
// descriptors).  Every merged page's content stream is copied at its original
// jsreport size (~6 MB/chunk raw), so base.pdf grew by ~6 MB per chunk even
// though each compressed chunk file was only ~1 MB.
//
// FIX — TWO-STAGE ACCUMULATION
// ──────────────────────────────
// Stage 1 (merge)  : PdfMerger appends the new chunk into base.pdf as before.
//                    This is fast but the file grows by the raw chunk size.
// Stage 2 (repack) : After every merge we call ReportUtils.CompressPdf() on the
//                    ENTIRE base.pdf byte array.  CompressPdf performs a real
//                    re-encode (GhostScript or iText full-rewrite), reducing the
//                    accumulator from ~6 MB/chunk growth to ~1 MB/chunk.
//
// CAP AT 30 MB
// ─────────────
// If the repacked base.pdf still exceeds MaxBasePdfBytes (30 MB) we additionally
// force a second repack with BEST_COMPRESSION.  In practice this never fires
// because the first CompressPdf call keeps the file well under 30 MB, but it
// acts as a hard safety net.
//
// DEFERRED STAMPING (unchanged)
// ──────────────────────────────
// base.pdf is NEVER stamped during background work.  StampPdfToSnapshot() runs
// only when a client polls GET /progressive/{jobId}, writes to a one-shot temp
// file, and deletes it after the bytes are read — so the expensive iText layout
// pass is capped to one execution per client request, not per chunk.

//using iText.Kernel.Colors;
//using iText.Kernel.Font;
//using iText.Kernel.Pdf;
//using iText.Kernel.Utils;
//using iText.Layout;
//using iText.Layout.Element;
//using iText.Layout.Properties;
//using jsreport.AspNetCore;
//using jsreport.Types;
//using Microsoft.Extensions.Caching.Memory;
//using NexgenCosysReport.Dtos.ReportDtos;
//using NexgenCosysReport.Inteface.ReportInterface;
//using NexgenCosysReport.Utils.Report;
//using System.Diagnostics;
//using CompressionConstants = iText.Kernel.Pdf.CompressionConstants;

//namespace NexgenCosysReport.Services.ReportService
//{
//    public class ProgressivePdfService : IProgressivePdfService
//    {
//        private readonly IRazorRenderService _razor;
//        private readonly IJsReportMVCService _jsReport;
//        private readonly IMemoryCache _cache;
//        private readonly ILogger<ProgressivePdfService> _logger;
//        private const int RenderTimeoutMs = 600_000;

//        // ── Batch strategy constants ─────────────────────────────────────────
//        // Number of chunk files to render before a single merge flush into base.pdf.
//        // Each chunk is ~330 KB, so batchSize=5 → ~1.6 MB input per merge (constant).
//        // Raise for fewer merges; lower for more frequent progress updates.
//        private const int BatchSize = 5;

//        // Re-compress the accumulated base.pdf every N batch flushes (0 = never).
//        // Compression is expensive but keeps the file small for snapshot downloads.
//        private const int CompressEveryNBatches = 5;

//        public ProgressivePdfService(
//            IRazorRenderService razor,
//            IJsReportMVCService jsReport,
//            IMemoryCache cache,
//            ILogger<ProgressivePdfService> logger)
//        {
//            _razor = razor;
//            _jsReport = jsReport;
//            _cache = cache;
//            _logger = logger;
//        }

//        // ────────────────────────────────────────────────────────────────────
//        // PUBLIC API
//        // ────────────────────────────────────────────────────────────────────

//        public async Task<ProgressivePdfJob> StartAsync(
//            string reportPath,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            PageSizeSetting? pageSetting,
//            CancellationToken ct,
//            int chunkSize = 500,
//            int maxParallelism = 3)
//        {
//            if (!reportData.TryGetValue(rowsKey, out var rowsObj) ||
//                rowsObj is not IEnumerable<object> rowsEnum)
//                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

//            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
//            if (allRows.Count == 0)
//                throw new ArgumentException("Row collection is empty.");

//            var tempDir = Path.Combine(
//                Path.GetTempPath(), "nexgen_progressive", Guid.NewGuid().ToString("N"));
//            Directory.CreateDirectory(tempDir);

//            var job = new ProgressivePdfJob
//            {
//                TempDir = tempDir,
//                TotalRows = allRows.Count,
//                EstimatedTotalPages = (int)Math.Ceiling(allRows.Count / 25.0),
//                BasePdfPath = Path.Combine(tempDir, "base.pdf")
//            };

//            _cache.Set(CacheKey(job.JobId), job, new MemoryCacheEntryOptions
//            {
//                SlidingExpiration = TimeSpan.FromMinutes(30),
//                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120)
//            }.RegisterPostEvictionCallback((_, val, _, _) =>
//            {
//                if (val is ProgressivePdfJob j) TryCleanup(j.TempDir);
//            }));

//            _logger.LogInformation(
//                "🚀 Job {Id} started — {Rows} rows, chunkSize={Chunk}, parallelism={Par}",
//                job.JobId, allRows.Count, chunkSize, maxParallelism);

//            var chunks = PartitionRows(allRows, chunkSize);
//            job.TotalChunks = chunks.Count;

//            // First chunk (with header) – rendered synchronously so the caller
//            // gets a valid base.pdf immediately, using the request cancellation token.
//            var firstChunkPath = Path.Combine(job.TempDir, "chunk_00000.pdf");
//            await RenderChunkToFileAsync(
//                chunks[0], reportData, rowsKey, reportPath,
//                pageSetting, firstChunkPath, ct, showHeader: true);

//            File.Copy(firstChunkPath, job.BasePdfPath, overwrite: true);
//            TryDelete(firstChunkPath);

//            job.CompletedChunks = 1;
//            job.PagesReady = CountPages(job.BasePdfPath);
//            job.CurrentSizeBytes = new FileInfo(job.BasePdfPath).Length;
//            job.LastUpdated = DateTime.UtcNow;

//            // Remaining chunks – background, not tied to the HTTP request token.
//            _ = Task.Run(async () =>
//            {
//                try
//                {
//                    await ProcessRemainingChunksAsync(
//                        job,
//                        chunks.Skip(1).ToList(),
//                        reportData, rowsKey,
//                        reportPath, pageSetting,
//                        maxParallelism,
//                        CancellationToken.None);
//                }
//                catch (Exception ex)
//                {
//                    job.HasError = true;
//                    job.ErrorMessage = ex.Message;
//                    _logger.LogError(ex, "❌ Job {Id} background processing failed", job.JobId);
//                }
//            });

//            return job;
//        }

//        public ProgressivePdfJob? GetJob(string jobId)
//            => _cache.TryGetValue(CacheKey(jobId), out ProgressivePdfJob? job) ? job : null;

//        public async Task<(byte[] Bytes, ProgressivePdfJob Job)> GetSnapshotAsync(
//            string jobId, CancellationToken ct)
//        {
//            var job = GetJob(jobId)
//                ?? throw new KeyNotFoundException($"Job {jobId} not found or expired.");

//            await job.FileLock.WaitAsync(ct).ConfigureAwait(false);
//            try
//            {
//                if (!File.Exists(job.BasePdfPath))
//                    throw new FileNotFoundException("base.pdf not ready yet.", job.BasePdfPath);

//                var snapshotPath = Path.Combine(job.TempDir, $"snap_{Guid.NewGuid():N}.pdf");
//                try
//                {
//                    StampPdfToSnapshot(job.BasePdfPath, snapshotPath);
//                    return (await File.ReadAllBytesAsync(snapshotPath, ct).ConfigureAwait(false), job);
//                }
//                finally
//                {
//                    TryDelete(snapshotPath);
//                }
//            }
//            finally
//            {
//                job.FileLock.Release();
//            }
//        }

//        // ────────────────────────────────────────────────────────────────────
//        // CORE BACKGROUND PROCESSOR  (batch-flush strategy)
//        // ────────────────────────────────────────────────────────────────────

//        private async Task ProcessRemainingChunksAsync(
//            ProgressivePdfJob job,
//            IList<IList<object>> remainingChunks,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            string reportPath,
//            PageSizeSetting? pageSetting,
//            int maxParallelism,
//            CancellationToken ct)
//        {
//            var sw = Stopwatch.StartNew();

//            // Split the remaining chunks into fixed-size batches.
//            // Each batch is rendered fully in parallel, then merged into base.pdf
//            // in ONE write — so merge input size is always N × ~330 KB (constant).
//            var batches = PartitionChunks(remainingChunks, BatchSize);

//            using var throttle = new SemaphoreSlim(maxParallelism);

//            for (int b = 0; b < batches.Count; b++)
//            {
//                var batch = batches[b];
//                var chunkPaths = new string?[batch.Count];

//                // ── Phase 1: render all chunks in this batch concurrently ────
//                var renderTasks = batch
//                    .Select((chunk, localIdx) => Task.Run(async () =>
//                    {
//                        // Global chunk index (1-based) for unique filenames.
//                        int globalIdx = b * BatchSize + localIdx + 1;

//                        await throttle.WaitAsync(ct).ConfigureAwait(false);
//                        try
//                        {
//                            var path = Path.Combine(
//                                job.TempDir, $"chunk_{globalIdx:D5}.pdf");

//                            await RenderChunkToFileAsync(
//                                chunk, reportData, rowsKey,
//                                reportPath, pageSetting, path, ct,
//                                showHeader: false).ConfigureAwait(false);

//                            chunkPaths[localIdx] = path;
//                        }
//                        finally
//                        {
//                            throttle.Release();
//                        }
//                    }, ct))
//                    .ToList();

//                try
//                {
//                    await Task.WhenAll(renderTasks).ConfigureAwait(false);
//                }
//                catch (Exception ex)
//                {
//                    job.HasError = true;
//                    job.ErrorMessage = $"Batch {b + 1} render: {ex.Message}";
//                    _logger.LogError(ex, "❌ Job {Id} batch {B} render failed", job.JobId, b + 1);
//                    break;
//                }

//                // ── Phase 2: single merge of the whole batch into base.pdf ───
//                // Input is always BatchSize × ~330 KB regardless of which batch
//                // number this is → merge time stays flat across all 100 batches.
//                bool compress = CompressEveryNBatches > 0
//                    && ((b + 1) % CompressEveryNBatches == 0
//                        || (b + 1) == batches.Count);

//                await job.FileLock.WaitAsync(ct).ConfigureAwait(false);
//                try
//                {
//                    var validPaths = chunkPaths.Where(p => p != null).Select(p => p!).ToArray();
//                    await MergeBatchIntoBaseAsync(
//                        job.BasePdfPath, validPaths, _logger, ct, compress)
//                        .ConfigureAwait(false);
//                }
//                catch (Exception ex)
//                {
//                    job.HasError = true;
//                    job.ErrorMessage = $"Batch {b + 1} merge: {ex.Message}";
//                    _logger.LogError(ex, "❌ Job {Id} batch {B} merge failed", job.JobId, b + 1);
//                }
//                finally
//                {
//                    job.FileLock.Release();
//                }

//                // ── Phase 3: update progress & clean up chunk temp files ─────
//                lock (job)
//                {
//                    job.CompletedChunks += batch.Count;
//                    job.PagesReady = CountPages(job.BasePdfPath);
//                    job.CurrentSizeBytes = new FileInfo(job.BasePdfPath).Length;
//                    job.LastUpdated = DateTime.UtcNow;
//                }

//                foreach (var p in chunkPaths)
//                    TryDelete(p);

//                _logger.LogInformation(
//                    "📄 Job {Id} flushed batch {B}/{Tot} ({N} chunks) in {E:F1}s",
//                    job.JobId, b + 1, batches.Count, batch.Count, sw.Elapsed.TotalSeconds);

//                if (job.HasError) break;
//            }

//            job.IsComplete = true;
//            _logger.LogInformation(
//                "✅ Job {Id} complete — {Pages} pages, {MB:F2} MB in {E:F1}s",
//                job.JobId,
//                job.PagesReady,
//                job.CurrentSizeBytes / 1024.0 / 1024.0,
//                sw.Elapsed.TotalSeconds);
//        }

//        // ────────────────────────────────────────────────────────────────────
//        // RENDERING
//        // ────────────────────────────────────────────────────────────────────

//        private async Task RenderChunkToFileAsync(
//            IList<object> chunkRows,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            string reportPath,
//            PageSizeSetting? pageSetting,
//            string outputPath,
//            CancellationToken ct,
//            bool showHeader)
//        {
//            var chunkData = new Dictionary<string, object>(reportData)
//            {
//                [rowsKey] = chunkRows,
//                ["ShowHeader"] = showHeader
//            };

//            var html = await _razor.RenderToStringAsync(reportPath, chunkData)
//                .ConfigureAwait(false);

//            await RenderHtmlToCompressedFileAsync(html, pageSetting, outputPath, ct)
//                .ConfigureAwait(false);
//        }

//        private async Task RenderHtmlToCompressedFileAsync(
//            string html,
//            PageSizeSetting? pageSetting,
//            string outputPath,
//            CancellationToken ct)
//        {
//            var request = new RenderRequest
//            {
//                Template = new Template
//                {
//                    Content = html,
//                    Engine = Engine.None,
//                    Recipe = JsReportTemplateHelper.GetRecipe("PDF")
//                },
//                Options = new RenderOptions { Timeout = RenderTimeoutMs }
//            };
//            JsReportTemplateHelper.ConfigureTemplate(request, "PDF", pageSetting, suppressFooter: true);

//            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);

//            using var ms = new MemoryStream();
//            await result.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
//            byte[] rawBytes = ms.TryGetBuffer(out var buffer) ? buffer.Array! : ms.ToArray();
//            byte[] compressed = ReportUtils.CompressPdf(rawBytes, _logger);
//            await File.WriteAllBytesAsync(outputPath, compressed, ct).ConfigureAwait(false);
//        }

//        // ────────────────────────────────────────────────────────────────────
//        // MERGE HELPERS
//        // ────────────────────────────────────────────────────────────────────

//        /// <summary>
//        /// Merges an entire batch of pre-rendered chunk files into base.pdf in
//        /// a single streaming pass.  Because input is always BatchSize × ~330 KB
//        /// (not the growing base), merge time is uniform across all batches.
//        /// </summary>
//        private static async Task MergeBatchIntoBaseAsync(
//            string basePath,
//            string[] chunkPaths,
//            ILogger logger,
//            CancellationToken ct,
//            bool compress)
//        {
//            var tempMerged = basePath + ".tmp";

//            // Open base + all chunks and write to a single temp file.
//            await using (var baseFs = new FileStream(
//                basePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
//            await using (var outFs = new FileStream(
//                tempMerged, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
//            {
//                using var baseDoc = new PdfDocument(new PdfReader(baseFs));
//                using var writer = new PdfWriter(outFs);
//                writer.SetCompressionLevel(CompressionConstants.DEFAULT_COMPRESSION);
//                using var outDoc = new PdfDocument(writer);
//                var merger = new PdfMerger(outDoc);

//                // Append existing base first.
//                merger.Merge(baseDoc, 1, baseDoc.GetNumberOfPages());

//                // Append each chunk in order — all opened/closed within this pass.
//                foreach (var chunkPath in chunkPaths)
//                {
//                    await using var cfs = new FileStream(
//                        chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
//                    using var cDoc = new PdfDocument(new PdfReader(cfs));
//                    merger.Merge(cDoc, 1, cDoc.GetNumberOfPages());
//                }
//            }

//            // Atomically replace base.pdf with the merged result.
//            File.Delete(basePath);
//            File.Move(tempMerged, basePath);

//            // Optional full-file compression to keep base.pdf small for snapshots.
//            if (compress)
//            {
//                var raw = await File.ReadAllBytesAsync(basePath, ct).ConfigureAwait(false);
//                var compressed = ReportUtils.CompressPdf(raw, logger);
//                await File.WriteAllBytesAsync(basePath, compressed, ct).ConfigureAwait(false);
//            }
//        }

//        // ────────────────────────────────────────────────────────────────────
//        // SNAPSHOT STAMPING
//        // ────────────────────────────────────────────────────────────────────

//        private static void StampPdfToSnapshot(string basePdfPath, string snapshotPath)
//        {
//            using var reader = new PdfReader(basePdfPath);
//            using var writer = new PdfWriter(snapshotPath);
//            using var pdfDoc = new PdfDocument(reader, writer);
//            using var layoutDoc = new Document(pdfDoc);
//            layoutDoc.SetMargins(0, 0, 0, 0);

//            int totalPages = pdfDoc.GetNumberOfPages();
//            var font = PdfFontFactory.CreateFont();

//            for (int i = 1; i <= totalPages; i++)
//            {
//                var size = pdfDoc.GetPage(i).GetPageSizeWithRotation();
//                var para = new Paragraph($"{i} of {totalPages}")
//                    .SetFont(font)
//                    .SetFontSize(10)
//                    .SetFontColor(ColorConstants.BLACK)
//                    .SetMargin(0)
//                    .SetPadding(0);

//                layoutDoc.ShowTextAligned(
//                    para,
//                    size.GetWidth() / 2f,
//                    20f,
//                    i,
//                    TextAlignment.CENTER,
//                    VerticalAlignment.BOTTOM,
//                    0f);
//            }
//        }

//        // ────────────────────────────────────────────────────────────────────
//        // UTILITIES
//        // ────────────────────────────────────────────────────────────────────

//        /// <summary>Splits a flat row list into fixed-size chunks for rendering.</summary>
//        private static List<IList<object>> PartitionRows(IList<object> source, int chunkSize)
//        {
//            var result = new List<IList<object>>((source.Count + chunkSize - 1) / chunkSize);
//            for (int i = 0; i < source.Count; i += chunkSize)
//            {
//                int count = Math.Min(chunkSize, source.Count - i);
//                var slice = new List<object>(count);
//                for (int j = 0; j < count; j++)
//                    slice.Add(source[i + j]);
//                result.Add(slice);
//            }
//            return result;
//        }

//        /// <summary>Splits a list of rendered chunks into fixed-size batches for merging.</summary>
//        private static List<IList<T>> PartitionChunks<T>(IList<T> source, int batchSize)
//        {
//            var result = new List<IList<T>>();
//            for (int i = 0; i < source.Count; i += batchSize)
//                result.Add(source.Skip(i).Take(batchSize).ToList());
//            return result;
//        }

//        private static int CountPages(string pdfPath)
//        {
//            try
//            {
//                using var reader = new PdfReader(pdfPath);
//                using var doc = new PdfDocument(reader);
//                return doc.GetNumberOfPages();
//            }
//            catch
//            {
//                return 0;
//            }
//        }

//        private static string CacheKey(string id) => $"progressive_pdf_{id}";

//        private static void TryDelete(string? path)
//        {
//            try
//            {
//                if (!string.IsNullOrEmpty(path) && File.Exists(path))
//                    File.Delete(path);
//            }
//            catch { /* best-effort */ }
//        }

//        private static void TryCleanup(string dir)
//        {
//            try
//            {
//                if (Directory.Exists(dir))
//                    Directory.Delete(dir, recursive: true);
//            }
//            catch { /* best-effort */ }
//        }
//    }
//}




//latest best of all
//latest best of all
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using jsreport.AspNetCore;
using jsreport.Types;
using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Utils.Report;
using System.Diagnostics;
using System.Threading.Channels;
using CompressionConstants = iText.Kernel.Pdf.CompressionConstants;

namespace NexgenCosysReport.Services.ReportService
{
    public sealed class ProgressivePdfService : IProgressivePdfService, IDisposable
    {
        // ── Dependencies ────────────────────────────────────────────────────
        private readonly IRazorRenderService _razor;
        private readonly IJsReportMVCService _jsReport;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProgressivePdfService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        // ════════════════════════════════════════════════════════════════════
        //  TUNING  — all in one place for easy adjustment
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Number of compressed chunks merged into one slab (level-1 merge).
        /// Input per merge = BatchFlushSize × ~1.5 MB ≈ 7.5 MB (CONSTANT).
        /// Keep at 5; lowering increases flush frequency, raising delays previews.
        /// </summary>
        private const int BatchFlushSize = 5;

        /// <summary>
        /// Number of slabs accumulated into base.pdf (level-2 merge).
        /// Input per merge = SlabAccumulateSize × ~7.5 MB ≈ 30 MB (CONSTANT).
        /// </summary>
        private const int SlabAccumulateSize = 4;

        /// <summary>
        /// Compress each chunk immediately after render using ReportUtils.
        /// Adds ~200–400 ms per chunk but reduces disk I/O by 60–75%.
        /// Set false only if CPU is the bottleneck (unlikely for text PDFs).
        /// </summary>
        private const bool CompressChunksAfterRender = true;

        /// <summary>
        /// Recompress base.pdf every N level-2 accumulations. Set to 1 for always.
        /// Keeps base.pdf tight for snapshot serving.  0 = never.
        /// </summary>
        private const int RecompressBaseEveryN = 1;  // CHANGED: always recompress

        private const int RenderTimeoutMs = 600_000;
        private const int RenderRetryAttempts = 3;
        private const int MaxConcurrentJobs = 4;

        /// <summary>
        /// Maximum threads dedicated to iText merge work.
        /// Separate from ASP.NET pool to prevent Kestrel starvation.
        /// </summary>
        private const int MaxMergeThreads = 4;  // CHANGED: increased from 2

        private static readonly SemaphoreSlim GlobalJobThrottle =
            new(MaxConcurrentJobs, MaxConcurrentJobs);

        /// <summary>
        /// Dedicated semaphore for ALL iText merge operations across all jobs.
        /// Prevents thread-pool starvation (fixes the Kestrel heartbeat warning).
        /// </summary>
        private static readonly SemaphoreSlim MergeThrottle =
            new(MaxMergeThreads, MaxMergeThreads);

        // Updated based on real compressed chunk size from logs (~329 KB)
        private const long EstimatedCompressedBytesPerChunk = 329 * 1024; // ~329 KB

        private bool _disposed;

        // ════════════════════════════════════════════════════════════════════
        //  CONSTRUCTOR
        // ════════════════════════════════════════════════════════════════════

        public ProgressivePdfService(
            IRazorRenderService razor,
            IJsReportMVCService jsReport,
            IMemoryCache cache,
            ILogger<ProgressivePdfService> logger,
            IHostApplicationLifetime lifetime)
        {
            _razor = razor;
            _jsReport = jsReport;
            _cache = cache;
            _logger = logger;
            _lifetime = lifetime;
        }

        // ════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ════════════════════════════════════════════════════════════════════

        public async Task<ProgressivePdfJob> StartAsync(
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            PageSizeSetting? pageSetting,
            CancellationToken ct,
            int chunkSize = 500,
            int maxParallelism = 4)
        {
            // ── Validation ─────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(reportPath))
                throw new ArgumentException("reportPath is required.", nameof(reportPath));
            if (reportPath.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("reportPath must not traverse directories.", nameof(reportPath));
            if (chunkSize is <= 0 or > 10_000)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Must be 1–10 000.");
            if (maxParallelism is <= 0 or > 16)
                throw new ArgumentOutOfRangeException(nameof(maxParallelism), "Must be 1–16.");
            if (!reportData.TryGetValue(rowsKey, out var rowsObj) ||
                rowsObj is not IEnumerable<object> rowsEnum)
                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
            if (allRows.Count == 0)
                throw new ArgumentException("Row collection is empty.");

            // ── Disk pre-flight ────────────────────────────────────────────
            var tempRoot = Path.Combine(Path.GetTempPath(), "nexgen_progressive");
            Directory.CreateDirectory(tempRoot);
            EnsureSufficientDiskSpace(tempRoot, allRows.Count, chunkSize);

            var tempDir = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var chunks = PartitionRows(allRows, chunkSize);

            var job = new ProgressivePdfJob
            {
                TempDir = tempDir,
                TotalRows = allRows.Count,
                TotalChunks = chunks.Count,
                EstimatedTotalPages = (int)Math.Ceiling(allRows.Count / 25.0),
                HeaderChunkPath = Path.Combine(tempDir, "chunk_00000.pdf"),
                BasePdfPath = Path.Combine(tempDir, "base.pdf"),
            };

            RegisterCache(job);

            _logger.LogInformation(
                "🚀 Job {JobId} — {Rows} rows, {Chunks} chunks, " +
                "parallelism={Par}, chunkFlush={CF}, slabAccum={SA}",
                job.JobId, allRows.Count, chunks.Count,
                maxParallelism, BatchFlushSize, SlabAccumulateSize);

            // ── Render first chunk synchronously so caller gets immediate data
            await RenderChunkWithRetryAsync(
                chunks[0], reportData, rowsKey, reportPath, pageSetting,
                job.HeaderChunkPath, ct, showHeader: true)
                .ConfigureAwait(false);

            // Seed base.pdf from the (already compressed) header chunk.
            File.Copy(job.HeaderChunkPath, job.BasePdfPath, overwrite: true);

            job.IncrementCompletedChunks();
            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = SafeFileLength(job.BasePdfPath);
            job.LastUpdated = DateTime.UtcNow;

            // ── Background rendering pipeline ──────────────────────────────
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetime.ApplicationStopping,
                job.CancellationTokenSource.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    await GlobalJobThrottle.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                    try
                    {
                        await ProcessRemainingChunksAsync(
                            job, chunks, reportData, rowsKey,
                            reportPath, pageSetting, maxParallelism,
                            linkedCts.Token)
                            .ConfigureAwait(false);
                    }
                    finally { GlobalJobThrottle.Release(); }
                }
                catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
                {
                    job.HasError = true;
                    job.ErrorMessage = "Cancelled (host shutting down or job cancelled).";
                    _logger.LogWarning("⚠️  Job {JobId} cancelled", job.JobId);
                }
                catch (Exception ex)
                {
                    job.HasError = true;
                    job.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "❌ Job {JobId} pipeline failed", job.JobId);
                }
                finally { linkedCts.Dispose(); }
            }, CancellationToken.None);

            return job;
        }

        public ProgressivePdfJob? GetJob(string jobId)
            => _cache.TryGetValue(CacheKey(jobId), out ProgressivePdfJob? job) ? job : null;

        /// <summary>
        /// Fast snapshot: base.pdf (accumulated slabs) + un-accumulated slab tail
        /// + un-flushed chunk tail.  Cost = O(≤SlabAccumulateSize + ≤BatchFlushSize).
        /// </summary>
        public async Task<(byte[] Bytes, ProgressivePdfJob Job)> GetSnapshotAsync(
            string jobId, CancellationToken ct)
        {
            var job = GetJob(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found or expired.");

            if (!File.Exists(job.BasePdfPath))
                throw new FileNotFoundException("base.pdf not ready.", job.BasePdfPath);

            // Snapshot: base + any pending slabs + any pending chunks
            List<string> inputs;
            await job.FlushLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                inputs = BuildSnapshotInputList(job);
            }
            finally { job.FlushLock.Release(); }

            var snapPath = Path.Combine(job.TempDir, $"snap_{Guid.NewGuid():N}.pdf");
            try
            {
                await MergeOnMergeThreadAsync(inputs, snapPath,
                    CompressionLevel.Speed, stampPageNumbers: true, ct)
                    .ConfigureAwait(false);

                return (await File.ReadAllBytesAsync(snapPath, ct).ConfigureAwait(false), job);
            }
            finally { TryDelete(snapPath); }
        }

        /// <summary>Returns the fully merged, best-compressed final PDF.</summary>
        public async Task<(byte[] Bytes, ProgressivePdfJob Job)> GetFinalAsync(
            string jobId, CancellationToken ct)
        {
            var job = GetJob(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found or expired.");

            if (!job.IsComplete)
                throw new InvalidOperationException(
                    $"Job {jobId} is not complete ({job.CompletedChunks}/{job.TotalChunks}).");
            if (job.HasError)
                throw new InvalidOperationException($"Job {jobId} failed: {job.ErrorMessage}");

            // Ensure everything is flushed into base.pdf before final download.
            await FlushAllToBaseAsync(job, ct).ConfigureAwait(false);

            var finalPath = Path.Combine(job.TempDir, $"final_{Guid.NewGuid():N}.pdf");
            try
            {
                // Re-read base.pdf (now complete) with best compression + page numbers.
                await MergeOnMergeThreadAsync(
                    new[] { job.BasePdfPath }, finalPath,
                    CompressionLevel.Best, stampPageNumbers: true, ct)
                    .ConfigureAwait(false);

                return (await File.ReadAllBytesAsync(finalPath, ct).ConfigureAwait(false), job);
            }
            finally { TryDelete(finalPath); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  CORE BACKGROUND PIPELINE
        //
        //  Stage A — Render producer (parallelism=maxParallelism):
        //    Renders chunks concurrently; compresses each; signals channel.
        //
        //  Stage B — Slab flush consumer (single reader):
        //    Waits for BatchFlushSize signals; merges chunks → slab_NNNNN.pdf.
        //    Merge input: always BatchFlushSize × ~329 KB = CONSTANT.
        //
        //  Stage C — Base accumulation (inside slab flush):
        //    Every SlabAccumulateSize slabs, merges slabs → base.pdf.
        //    Merge input: always SlabAccumulateSize × ~1.6 MB = CONSTANT.
        // ════════════════════════════════════════════════════════════════════

        private async Task ProcessRemainingChunksAsync(
            ProgressivePdfJob job,
            IList<IList<object>> allChunks,
            IDictionary<string, object> reportData,
            string rowsKey,
            string reportPath,
            PageSizeSetting? pageSetting,
            int maxParallelism,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            using var renderThrottle = new SemaphoreSlim(maxParallelism, maxParallelism);

            // Unbounded channel: many render writers → single flush reader.
            var channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

            int slabSeq = 0; // monotonic slab sequence number
            int accumulCount = 0; // slabs since last level-2 accumulation

            // ── Stage A: render producer ────────────────────────────────────
            var renderProducer = Task.Run(async () =>
            {
                try
                {
                    var tasks = new List<Task>(allChunks.Count - 1);

                    for (int i = 1; i < allChunks.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        int idx = i;
                        var chunk = allChunks[i];

                        await renderThrottle.WaitAsync(ct).ConfigureAwait(false);

                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                var path = Path.Combine(job.TempDir, $"chunk_{idx:D5}.pdf");

                                await RenderChunkWithRetryAsync(
                                    chunk, reportData, rowsKey, reportPath,
                                    pageSetting, path, ct, showHeader: false)
                                    .ConfigureAwait(false);

                                job.PendingChunks[idx] = path;
                                job.IncrementCompletedChunks();
                                job.LastUpdated = DateTime.UtcNow;

                                await channel.Writer.WriteAsync(idx, ct).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "❌ Job {JobId} chunk {Idx} failed", job.JobId, idx);
                                throw;
                            }
                            finally { renderThrottle.Release(); }
                        }, ct));
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                finally { channel.Writer.TryComplete(); }
            }, ct);

            // ── Stage B: slab flush consumer ───────────────────────────────
            var flushConsumer = Task.Run(async () =>
            {
                int pendingSinceFlush = 0;

                await foreach (var _ in channel.Reader.ReadAllAsync(ct))
                {
                    pendingSinceFlush++;
                    bool isLast = job.CompletedChunks >= job.TotalChunks;

                    if (pendingSinceFlush >= BatchFlushSize || isLast)
                    {
                        await job.FlushLock.WaitAsync(ct).ConfigureAwait(false);
                        try
                        {
                            // Drain pending chunks → slab
                            var slabPath = await FlushChunksToSlabAsync(
                                job, ++slabSeq, ct)
                                .ConfigureAwait(false);

                            if (slabPath != null)
                            {
                                job.PendingSlabs[slabSeq] = slabPath;
                                accumulCount++;
                            }

                            // Accumulate slabs → base.pdf
                            bool shouldAccumulate =
                                accumulCount >= SlabAccumulateSize || isLast;

                            if (shouldAccumulate && !job.PendingSlabs.IsEmpty)
                            {
                                // CHANGED: always recompress (RecompressBaseEveryN = 1)
                                await AccumulateSlabsToBaseAsync(job, recompress: true, ct)
                                    .ConfigureAwait(false);

                                accumulCount = 0;
                            }
                        }
                        finally { job.FlushLock.Release(); }

                        pendingSinceFlush = 0;
                    }
                }

                // Drain any remainder after channel closes.
                if (!job.PendingChunks.IsEmpty || !job.PendingSlabs.IsEmpty)
                {
                    await job.FlushLock.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        if (!job.PendingChunks.IsEmpty)
                        {
                            var slabPath = await FlushChunksToSlabAsync(job, ++slabSeq, ct)
                                .ConfigureAwait(false);
                            if (slabPath != null)
                                job.PendingSlabs[slabSeq] = slabPath;
                        }

                        if (!job.PendingSlabs.IsEmpty)
                        {
                            await AccumulateSlabsToBaseAsync(job, recompress: true, ct)
                                .ConfigureAwait(false);
                        }
                    }
                    finally { job.FlushLock.Release(); }
                }
            }, ct);

            try
            {
                await Task.WhenAll(renderProducer, flushConsumer).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                job.HasError = true;
                job.ErrorMessage = ex.Message;
                _logger.LogError(ex, "❌ Job {JobId} aggregate failure", job.JobId);
                return;
            }

            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = SafeFileLength(job.BasePdfPath);
            job.IsComplete = true;

            _logger.LogInformation(
                "✅ Job {JobId} complete — {Pages} pp, {MB:F2} MB in {S:F1}s",
                job.JobId, job.PagesReady,
                job.CurrentSizeBytes / 1024.0 / 1024.0,
                sw.Elapsed.TotalSeconds);
        }

        // ════════════════════════════════════════════════════════════════════
        //  LEVEL-1 FLUSH: pending chunks → slab_NNNNN.pdf
        //  Input is ALWAYS ≤BatchFlushSize × ~329 KB regardless of batch#.
        // ════════════════════════════════════════════════════════════════════

        private async Task<string?> FlushChunksToSlabAsync(
            ProgressivePdfJob job,
            int slabSeq,
            CancellationToken ct)
        {
            var toFlush = new List<(int idx, string path)>();
            foreach (var kv in job.PendingChunks.OrderBy(x => x.Key))
            {
                if (job.PendingChunks.TryRemove(kv.Key, out var p))
                    toFlush.Add((kv.Key, p));
            }

            if (toFlush.Count == 0) return null;

            var sw = Stopwatch.StartNew();
            var slabPath = Path.Combine(job.TempDir, $"slab_{slabSeq:D5}.pdf");
            var inputs = toFlush.Select(t => t.path).ToList();

            // CHANGED: use Best compression for slabs to keep them small
            await MergeOnMergeThreadAsync(
                inputs, slabPath,
                CompressionLevel.Best,   // previously Speed
                stampPageNumbers: false,
                ct)
                .ConfigureAwait(false);

            // Slab is written — delete chunk files (disk reclaimed immediately).
            foreach (var (_, p) in toFlush) TryDelete(p);

            _logger.LogInformation(
                "🗂  Job {JobId} slab {Seq}: {N} chunks → {MB:F2} MB in {Ms} ms",
                job.JobId, slabSeq, toFlush.Count,
                SafeFileLength(slabPath) / 1024.0 / 1024.0,
                sw.ElapsedMilliseconds);

            return slabPath;
        }

        // ════════════════════════════════════════════════════════════════════
        //  LEVEL-2 ACCUMULATE: pending slabs → base.pdf
        //  Input is ALWAYS ≤SlabAccumulateSize × ~1.6 MB regardless of batch#.
        // ════════════════════════════════════════════════════════════════════

        private async Task AccumulateSlabsToBaseAsync(
            ProgressivePdfJob job,
            bool recompress,
            CancellationToken ct)
        {
            var toAccum = new List<(int seq, string path)>();
            foreach (var kv in job.PendingSlabs.OrderBy(x => x.Key))
            {
                if (job.PendingSlabs.TryRemove(kv.Key, out var p))
                    toAccum.Add((kv.Key, p));
            }

            if (toAccum.Count == 0) return;

            var sw = Stopwatch.StartNew();
            var tmpBase = job.BasePdfPath + ".accumulating";

            // Merge: existing base + new slabs → tmpBase
            var inputs = new List<string>(toAccum.Count + 1) { job.BasePdfPath };
            inputs.AddRange(toAccum.Select(t => t.path));

            var level = recompress ? CompressionLevel.Best : CompressionLevel.Speed;

            await MergeOnMergeThreadAsync(inputs, tmpBase, level,
                stampPageNumbers: false, ct)
                .ConfigureAwait(false);

            // Atomically replace base.pdf.
            File.Move(tmpBase, job.BasePdfPath, overwrite: true);

            // Delete slab files — baked into base.pdf.
            foreach (var (_, p) in toAccum) TryDelete(p);

            job.AccumulationCount++;
            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = SafeFileLength(job.BasePdfPath);
            job.LastUpdated = DateTime.UtcNow;

            _logger.LogInformation(
                "📦 Job {JobId} accumulated {N} slabs → base.pdf " +
                "({Pages} pp, {MB:F2} MB, recompress={R}) in {Ms} ms",
                job.JobId, toAccum.Count,
                job.PagesReady,
                job.CurrentSizeBytes / 1024.0 / 1024.0,
                recompress,
                sw.ElapsedMilliseconds);
        }

        // ════════════════════════════════════════════════════════════════════
        //  FLUSH EVERYTHING → base.pdf  (called by GetFinalAsync)
        // ════════════════════════════════════════════════════════════════════

        private async Task FlushAllToBaseAsync(ProgressivePdfJob job, CancellationToken ct)
        {
            await job.FlushLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Remaining chunks → slab
                if (!job.PendingChunks.IsEmpty)
                {
                    int slabSeq = (job.PendingSlabs.IsEmpty ? 0
                        : job.PendingSlabs.Keys.Max()) + 1;

                    var slabPath = await FlushChunksToSlabAsync(job, slabSeq, ct)
                        .ConfigureAwait(false);
                    if (slabPath != null)
                        job.PendingSlabs[slabSeq] = slabPath;
                }

                // Remaining slabs → base
                if (!job.PendingSlabs.IsEmpty)
                    await AccumulateSlabsToBaseAsync(job, recompress: true, ct)
                        .ConfigureAwait(false);
            }
            finally { job.FlushLock.Release(); }
        }

        // ════════════════════════════════════════════════════════════════════
        //  RENDERING  (jsreport → compressed chunk file)
        // ════════════════════════════════════════════════════════════════════

        private async Task RenderChunkWithRetryAsync(
            IList<object> chunkRows,
            IDictionary<string, object> reportData,
            string rowsKey,
            string reportPath,
            PageSizeSetting? pageSetting,
            string outputPath,
            CancellationToken ct,
            bool showHeader)
        {
            Exception? last = null;
            for (int attempt = 1; attempt <= RenderRetryAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await RenderChunkToFileAsync(
                        chunkRows, reportData, rowsKey,
                        reportPath, pageSetting, outputPath, ct, showHeader)
                        .ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (attempt < RenderRetryAttempts)
                {
                    last = ex;
                    var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                    _logger.LogWarning(ex,
                        "⚠️  Render attempt {A}/{Max} failed — retry in {Ms} ms",
                        attempt, RenderRetryAttempts, delay.TotalMilliseconds);
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
            throw new InvalidOperationException(
                $"Render failed after {RenderRetryAttempts} attempts.", last);
        }

        private async Task RenderChunkToFileAsync(
            IList<object> chunkRows,
            IDictionary<string, object> reportData,
            string rowsKey,
            string reportPath,
            PageSizeSetting? pageSetting,
            string outputPath,
            CancellationToken ct,
            bool showHeader)
        {
            var chunkData = new Dictionary<string, object>(reportData)
            {
                [rowsKey] = chunkRows,
                ["ShowHeader"] = showHeader
            };

            var html = await _razor.RenderToStringAsync(reportPath, chunkData)
                .ConfigureAwait(false);

            using var renderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            renderCts.CancelAfter(TimeSpan.FromMilliseconds(RenderTimeoutMs));

            var request = new RenderRequest
            {
                Template = new Template
                {
                    Content = html,
                    Engine = Engine.None,
                    Recipe = JsReportTemplateHelper.GetRecipe("PDF")
                },
                Options = new RenderOptions { Timeout = RenderTimeoutMs }
            };
            JsReportTemplateHelper.ConfigureTemplate(request, "PDF", pageSetting, suppressFooter: true);

            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);

            // ── Stream → memory → compress → disk (atomic) ────────────────
            using var ms = new MemoryStream(capacity: 1024 * 1024);
            await result.Content.CopyToAsync(ms, renderCts.Token).ConfigureAwait(false);
            var rawBytes = ms.TryGetBuffer(out var buf) ? buf.Array! : ms.ToArray();

            byte[] finalBytes;
            if (CompressChunksAfterRender)
            {
                // ReportUtils.CompressPdf: zlib-9 + full xref streams + XMP strip.
                // Reduces ~9–10 MB chunk → ~1.5–2 MB. Critical for constant merge cost.
                finalBytes = await Task.Run(
                    () => ReportUtils.CompressPdf(rawBytes, _logger), ct)
                    .ConfigureAwait(false);
            }
            else
            {
                finalBytes = rawBytes;
            }

            // Atomic write: tmp → rename.
            var tmp = outputPath + ".tmp";
            await File.WriteAllBytesAsync(tmp, finalBytes, ct).ConfigureAwait(false);
            File.Move(tmp, outputPath, overwrite: true);

            _logger.LogDebug(
                "✏️  Chunk {Path}: {Raw:F1} KB → {Compressed:F1} KB ({Pct:F0}% reduction)",
                Path.GetFileName(outputPath),
                rawBytes.Length / 1024.0,
                finalBytes.Length / 1024.0,
                CompressChunksAfterRender && finalBytes.Length < rawBytes.Length
                    ? (1.0 - (double)finalBytes.Length / rawBytes.Length) * 100 : 0);
        }

        // ════════════════════════════════════════════════════════════════════
        //  iText MERGE ENGINE
        //  — all iText operations run here, throttled by MergeThrottle
        //    to prevent ASP.NET thread-pool starvation (Kestrel heartbeat fix)
        // ════════════════════════════════════════════════════════════════════

        private enum CompressionLevel { Speed, Best }

        /// <summary>
        /// Acquires MergeThrottle, then runs iText merge on a thread-pool
        /// thread dedicated to CPU/disk work (not Kestrel I/O threads).
        /// </summary>
        private async Task MergeOnMergeThreadAsync(
            IReadOnlyList<string> inputPaths,
            string outputPath,
            CompressionLevel level,
            bool stampPageNumbers,
            CancellationToken ct)
        {
            if (inputPaths.Count == 0)
                throw new ArgumentException("No inputs to merge.", nameof(inputPaths));

            var sw = Stopwatch.StartNew();

            // Throttle: max MaxMergeThreads concurrent iText jobs across all jobs.
            await MergeThrottle.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var unstampedPath = stampPageNumbers ? outputPath + ".unstamped" : outputPath;

                // Task.Run with LongRunning hint prevents thread-pool saturation.
                await Task.Factory.StartNew(() =>
                {
                    using var outFs = new FileStream(unstampedPath, FileMode.Create,
                        FileAccess.Write, FileShare.None, 131_072);
                    using var writer = new PdfWriter(outFs, BuildWriterProperties(level));
                    using var outDoc = new PdfDocument(writer);
                    outDoc.GetWriter().SetSmartMode(true); // dedup fonts/images

                    var mergerProps = new PdfMergerProperties().SetCloseSrcDocuments(true);
                    var merger = new PdfMerger(outDoc, mergerProps);

                    foreach (var path in inputPaths)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (!File.Exists(path)) continue;

                        using var reader = new PdfReader(path);
                        reader.SetUnethicalReading(true);
                        using var inDoc = new PdfDocument(reader);
                        merger.Merge(inDoc, 1, inDoc.GetNumberOfPages());
                    }

                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ConfigureAwait(false);

                if (stampPageNumbers)
                {
                    await Task.Factory.StartNew(
                        () => StampPageNumbers(unstampedPath, outputPath),
                        ct, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                        .ConfigureAwait(false);

                    TryDelete(unstampedPath);
                }
            }
            finally { MergeThrottle.Release(); }

            _logger.LogDebug(
                "🧩 Merge {N} → {Out} ({MB:F2} MB) in {Ms} ms",
                inputPaths.Count, Path.GetFileName(outputPath),
                SafeFileLength(outputPath) / 1024.0 / 1024.0,
                sw.ElapsedMilliseconds);
        }

        private static WriterProperties BuildWriterProperties(CompressionLevel level)
        {
            var wp = new WriterProperties();
            // CHANGED: always enable FullCompressionMode for size reduction
            wp.SetFullCompressionMode(true);
            if (level == CompressionLevel.Best)
            {
                wp.SetCompressionLevel(CompressionConstants.BEST_COMPRESSION);
            }
            else
            {
                wp.SetCompressionLevel(CompressionConstants.BEST_SPEED);
            }
            return wp;
        }

        // ════════════════════════════════════════════════════════════════════
        //  SNAPSHOT INPUT LIST BUILDER
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the ordered input list for a snapshot:
        ///   base.pdf + pending slabs + pending chunks
        /// Called under FlushLock.
        /// </summary>
        private static List<string> BuildSnapshotInputList(ProgressivePdfJob job)
        {
            var list = new List<string> { job.BasePdfPath };

            list.AddRange(
                job.PendingSlabs
                   .OrderBy(kv => kv.Key)
                   .Select(kv => kv.Value)
                   .Where(File.Exists));

            list.AddRange(
                job.PendingChunks
                   .OrderBy(kv => kv.Key)
                   .Select(kv => kv.Value)
                   .Where(File.Exists));

            return list;
        }

        // ═════════════════════════════════════════

        // ════════════════════════════════════════════════════════════════════
        //  PAGE-NUMBER STAMPING
        // ════════════════════════════════════════════════════════════════════

        private static void StampPageNumbers(string sourcePath, string outputPath)
        {
            using var reader = new PdfReader(sourcePath);
            using var writer = new PdfWriter(outputPath);
            using var pdfDoc = new PdfDocument(reader, writer);
            using var layout = new Document(pdfDoc);
            layout.SetMargins(0, 0, 0, 0);

            int total = pdfDoc.GetNumberOfPages();
            var font = PdfFontFactory.CreateFont();

            for (int i = 1; i <= total; i++)
            {
                var size = pdfDoc.GetPage(i).GetPageSizeWithRotation();
                layout.ShowTextAligned(
                    new Paragraph($"{i} of {total}")
                        .SetFont(font).SetFontSize(10)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetMargin(0).SetPadding(0),
                    size.GetWidth() / 2f, 20f, i,
                    TextAlignment.CENTER, VerticalAlignment.BOTTOM, 0f);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UTILITIES
        // ════════════════════════════════════════════════════════════════════

        private static List<IList<object>> PartitionRows(IList<object> source, int chunkSize)
        {
            var result = new List<IList<object>>(
                (source.Count + chunkSize - 1) / chunkSize);

            for (int i = 0; i < source.Count; i += chunkSize)
            {
                int count = Math.Min(chunkSize, source.Count - i);
                var slice = new List<object>(count);
                for (int j = 0; j < count; j++) slice.Add(source[i + j]);
                result.Add(slice);
            }
            return result;
        }

        private static int CountPages(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return 0;
            try
            {
                using var r = new PdfReader(path);
                using var d = new PdfDocument(r);
                return d.GetNumberOfPages();
            }
            catch { return 0; }
        }

        private static long SafeFileLength(string? path)
        {
            try { return string.IsNullOrEmpty(path) ? 0L : new FileInfo(path).Length; }
            catch { return 0L; }
        }

        private void EnsureSufficientDiskSpace(string directory, int totalRows, int chunkSize)
        {
            try
            {
                int chunks = (int)Math.Ceiling((double)totalRows / chunkSize);
                long required = EstimatedCompressedBytesPerChunk * chunks * 4; // 4× safety
                var drive = new DriveInfo(Path.GetPathRoot(directory)!);
                if (drive.AvailableFreeSpace < required)
                    throw new IOException(
                        $"Insufficient disk space: need ~{required / 1024 / 1024} MB, " +
                        $"have {drive.AvailableFreeSpace / 1024 / 1024} MB.");
            }
            catch (IOException) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️  Disk space check failed — proceeding");
            }
        }

        private void RegisterCache(ProgressivePdfJob job)
        {
            _cache.Set(CacheKey(job.JobId), job, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120)
            }.RegisterPostEvictionCallback((_, val, reason, _) =>
            {
                if (val is ProgressivePdfJob j)
                {
                    _logger.LogInformation("🧹 Evicting job {JobId} ({Reason})", j.JobId, reason);
                    TryCleanup(j.TempDir);
                    j.Dispose();
                }
            }));
        }

        private static string CacheKey(string id) => $"progressive_pdf_{id}";

        private static void TryDelete(string? path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
            catch { /* best-effort */ }
        }

        private static void TryCleanup(string dir)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
            catch { /* best-effort */ }
        }

        // ════════════════════════════════════════════════════════════════════
        //  DISPOSAL
        // ════════════════════════════════════════════════════════════════════

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // MergeThrottle is static (process-lifetime) — not disposed here.
        }
    }
}