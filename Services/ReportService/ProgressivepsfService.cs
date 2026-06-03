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
        private readonly IRazorRenderService _razor;
        private readonly IJsReportMVCService _jsReport;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProgressivePdfService> _logger;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly string _tempRoot;

        private const int BatchFlushSize = 5;
        private const int SlabAccumulateSize = 4;
        private const bool CompressChunksAfterRender = true;
        private const int RenderTimeoutMs = 600_000;
        private const int RenderRetryAttempts = 3;
        private const int MaxConcurrentJobs = 4;
        private const int MaxMergeThreads = 4;
        private static readonly SemaphoreSlim GlobalJobThrottle = new(MaxConcurrentJobs, MaxConcurrentJobs);
        private static readonly SemaphoreSlim MergeThrottle = new(MaxMergeThreads, MaxMergeThreads);
        private const long EstimatedCompressedBytesPerChunk = 329 * 1024;

        private bool _disposed;

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

            _tempRoot = Path.Combine(Path.GetTempPath(), "nexgen_progressive");
            Directory.CreateDirectory(_tempRoot);
            CleanupUnusedFolders(TimeSpan.FromHours(1));
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        public async Task<ProgressivePdfJob> StartAsync(
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            PageSizeSetting? pageSetting,
            CancellationToken ct,
            int chunkSize = 500,
            int maxParallelism = 4)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
                throw new ArgumentException("reportPath is required.", nameof(reportPath));
            if (reportPath.Contains("..", StringComparison.Ordinal))
                throw new ArgumentException("reportPath must not traverse directories.", nameof(reportPath));
            if (chunkSize is <= 0 or > 10_000)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Must be 1–10 000.");
            if (maxParallelism is <= 0 or > 16)
                throw new ArgumentOutOfRangeException(nameof(maxParallelism), "Must be 1–16.");
            if (!reportData.TryGetValue(rowsKey, out var rowsObj) || rowsObj is not IEnumerable<object> rowsEnum)
                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
            if (allRows.Count == 0)
                throw new ArgumentException("Row collection is empty.");

            CleanupUnusedFolders(TimeSpan.FromHours(1));
            EnsureSufficientDiskSpace(_tempRoot, allRows.Count, chunkSize);

            var tempDir = Path.Combine(_tempRoot, Guid.NewGuid().ToString("N"));
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

            _logger.LogInformation("🚀 Job {JobId} — {Rows} rows, {Chunks} chunks", job.JobId, allRows.Count, chunks.Count);

            await RenderChunkWithRetryAsync(chunks[0], reportData, rowsKey, reportPath, pageSetting,
                job.HeaderChunkPath, ct, showHeader: true).ConfigureAwait(false);

            File.Copy(job.HeaderChunkPath, job.BasePdfPath, overwrite: true);
            job.IncrementCompletedChunks();
            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = SafeFileLength(job.BasePdfPath);
            job.LastUpdated = DateTime.UtcNow;

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetime.ApplicationStopping, job.CancellationTokenSource.Token, ct);

            _ = Task.Run(async () =>
            {
                try
                {
                    await GlobalJobThrottle.WaitAsync(linkedCts.Token).ConfigureAwait(false);
                    try
                    {
                        await ProcessRemainingChunksAsync(job, chunks, reportData, rowsKey,
                            reportPath, pageSetting, maxParallelism, linkedCts.Token).ConfigureAwait(false);
                    }
                    finally { GlobalJobThrottle.Release(); }
                }
                catch (OperationCanceledException)
                {
                    job.HasError = true;
                    job.ErrorMessage = "Cancelled (request closed or host stopping).";
                    _logger.LogWarning("⚠️ Job {JobId} cancelled – cleaning up temp folder", job.JobId);
                    TryCleanup(job.TempDir, retries: 3);
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

        public async Task<(byte[] Bytes, ProgressivePdfJob Job)> GetSnapshotAsync(string jobId, CancellationToken ct)
        {
            var job = GetJob(jobId) ?? throw new KeyNotFoundException($"Job {jobId} not found or expired.");
            if (!File.Exists(job.BasePdfPath))
                throw new FileNotFoundException("base.pdf not ready.", job.BasePdfPath);

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
                await MergeOnMergeThreadAsync(inputs, snapPath, CompressionLevel.Speed, true, ct).ConfigureAwait(false);
                return (await File.ReadAllBytesAsync(snapPath, ct).ConfigureAwait(false), job);
            }
            finally { TryDelete(snapPath); }
        }

        public async Task<(byte[] Bytes, ProgressivePdfJob Job)> GetFinalAsync(string jobId, CancellationToken ct)
        {
            var job = GetJob(jobId) ?? throw new KeyNotFoundException($"Job {jobId} not found or expired.");
            if (!job.IsComplete)
                throw new InvalidOperationException($"Job {jobId} is not complete ({job.CompletedChunks}/{job.TotalChunks}).");
            if (job.HasError)
                throw new InvalidOperationException($"Job {jobId} failed: {job.ErrorMessage}");

            if (string.IsNullOrEmpty(job.FinalPdfPath) || !File.Exists(job.FinalPdfPath))
                throw new FileNotFoundException("Final PDF not generated yet. Please wait for completion.");

            byte[] bytes = await File.ReadAllBytesAsync(job.FinalPdfPath, ct).ConfigureAwait(false);
            return (bytes, job);
        }

        public void CleanupOrphanedFolders(TimeSpan deleteIfOlderThan)
            => CleanupUnusedFolders(deleteIfOlderThan);

        // =====================================================================
        // CLEANUP LOGIC
        // =====================================================================

        private void CleanupUnusedFolders(TimeSpan olderThan)
        {
            if (!Directory.Exists(_tempRoot)) return;

            var now = DateTime.UtcNow;
            foreach (var dir in Directory.GetDirectories(_tempRoot))
            {
                var dirInfo = new DirectoryInfo(dir);
                if (now - dirInfo.CreationTimeUtc > olderThan && !IsJobActive(dir))
                {
                    TryCleanup(dir, retries: 3);
                    _logger.LogInformation("🧹 Deleted old/unused folder: {Dir}", dir);
                }
            }
        }

        private bool IsJobActive(string folderPath)
        {
            var folderName = Path.GetFileName(folderPath);
            return _cache.TryGetValue(CacheKey(folderName), out _);
        }

        private static void TryCleanup(string dir, int retries = 3)
        {
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                            File.SetAttributes(file, FileAttributes.Normal);
                        Directory.Delete(dir, recursive: true);
                        return;
                    }
                }
                catch (UnauthorizedAccessException) when (i < retries - 1) { Thread.Sleep(1000); }
                catch (DirectoryNotFoundException) { return; }
                catch (IOException) when (i < retries - 1) { Thread.Sleep(500); }
                catch { return; }
            }
        }

        private static void TryDelete(string? path, int retries = 3)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            for (int i = 0; i < retries; i++)
            {
                try { File.Delete(path); return; }
                catch (UnauthorizedAccessException) when (i < retries - 1) { Thread.Sleep(500); }
                catch (IOException) when (i < retries - 1) { Thread.Sleep(500); }
                catch { return; }
            }
        }

        // =====================================================================
        // CORE PROGRESSIVE LOGIC
        // =====================================================================

        private enum CompressionLevel { Speed, Best }

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
            var channel = Channel.CreateUnbounded<int>(new UnboundedChannelOptions
            {
                SingleReader = true,
                AllowSynchronousContinuations = false
            });

            int slabSeq = 0;
            int accumulCount = 0;

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
                                await RenderChunkWithRetryAsync(chunk, reportData, rowsKey, reportPath,
                                    pageSetting, path, ct, showHeader: false).ConfigureAwait(false);

                                job.PendingChunks[idx] = path;
                                job.IncrementCompletedChunks();
                                job.LastUpdated = DateTime.UtcNow;
                                await channel.Writer.WriteAsync(idx, ct).ConfigureAwait(false);
                            }
                            finally { renderThrottle.Release(); }
                        }, ct));
                    }
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                finally { channel.Writer.TryComplete(); }
            }, ct);

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
                            var slabPath = await FlushChunksToSlabAsync(job, ++slabSeq, ct).ConfigureAwait(false);
                            if (slabPath != null)
                            {
                                job.PendingSlabs[slabSeq] = slabPath;
                                accumulCount++;
                            }

                            bool shouldAccumulate = accumulCount >= SlabAccumulateSize || isLast;
                            if (shouldAccumulate && !job.PendingSlabs.IsEmpty)
                            {
                                await AccumulateSlabsToBaseAsync(job, true, ct).ConfigureAwait(false);
                                accumulCount = 0;
                            }
                        }
                        finally { job.FlushLock.Release(); }
                        pendingSinceFlush = 0;
                    }
                }

                if (!job.PendingChunks.IsEmpty || !job.PendingSlabs.IsEmpty)
                {
                    await job.FlushLock.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        if (!job.PendingChunks.IsEmpty)
                        {
                            var slabPath = await FlushChunksToSlabAsync(job, ++slabSeq, ct).ConfigureAwait(false);
                            if (slabPath != null) job.PendingSlabs[slabSeq] = slabPath;
                        }
                        if (!job.PendingSlabs.IsEmpty)
                            await AccumulateSlabsToBaseAsync(job, true, ct).ConfigureAwait(false);
                    }
                    finally { job.FlushLock.Release(); }
                }
            }, ct);

            await Task.WhenAll(renderProducer, flushConsumer).ConfigureAwait(false);

            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = SafeFileLength(job.BasePdfPath);
            job.IsComplete = true;

            // Generate final PDF (once) – compressed and stamped
            var finalPath = Path.Combine(job.TempDir, "final.pdf");
            await MergeOnMergeThreadAsync(new[] { job.BasePdfPath }, finalPath, CompressionLevel.Best, true, ct).ConfigureAwait(false);
            job.FinalPdfPath = finalPath;
            job.CurrentSizeBytes = SafeFileLength(finalPath);

            _logger.LogInformation("✅ Job {JobId} complete — {Pages} pp, {MB:F2} MB in {S:F1}s",
                job.JobId, job.PagesReady, job.CurrentSizeBytes / 1024.0 / 1024.0, sw.Elapsed.TotalSeconds);
        }

        private async Task<string?> FlushChunksToSlabAsync(ProgressivePdfJob job, int slabSeq, CancellationToken ct)
        {
            var toFlush = new List<(int idx, string path)>();
            foreach (var kv in job.PendingChunks.OrderBy(x => x.Key))
            {
                if (job.PendingChunks.TryRemove(kv.Key, out var p))
                    toFlush.Add((kv.Key, p));
            }

            if (toFlush.Count == 0) return null;

            var slabPath = Path.Combine(job.TempDir, $"slab_{slabSeq:D5}.pdf");
            var inputs = toFlush.Select(t => t.path).ToList();
            await MergeOnMergeThreadAsync(inputs, slabPath, CompressionLevel.Best, false, ct).ConfigureAwait(false);
            foreach (var (_, p) in toFlush) TryDelete(p);
            _logger.LogInformation("🗂 Job {JobId} slab {Seq}: {N} chunks → {MB:F2} MB",
                job.JobId, slabSeq, toFlush.Count, SafeFileLength(slabPath) / 1024.0 / 1024.0);
            return slabPath;
        }

        private async Task AccumulateSlabsToBaseAsync(ProgressivePdfJob job, bool recompress, CancellationToken ct)
        {
            var toAccum = new List<(int seq, string path)>();
            foreach (var kv in job.PendingSlabs.OrderBy(x => x.Key))
            {
                if (job.PendingSlabs.TryRemove(kv.Key, out var p))
                    toAccum.Add((kv.Key, p));
            }

            if (toAccum.Count == 0) return;

            var tmpBase = job.BasePdfPath + ".accumulating";
            var inputs = new List<string> { job.BasePdfPath };
            inputs.AddRange(toAccum.Select(t => t.path));
            var level = recompress ? CompressionLevel.Best : CompressionLevel.Speed;
            await MergeOnMergeThreadAsync(inputs, tmpBase, level, false, ct).ConfigureAwait(false);
            File.Move(tmpBase, job.BasePdfPath, overwrite: true);
            foreach (var (_, p) in toAccum) TryDelete(p);
            job.AccumulationCount++;
            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = SafeFileLength(job.BasePdfPath);
            job.LastUpdated = DateTime.UtcNow;
            _logger.LogInformation("📦 Job {JobId} accumulated {N} slabs → base.pdf ({Pages} pp, {MB:F2} MB)",
                job.JobId, toAccum.Count, job.PagesReady, job.CurrentSizeBytes / 1024.0 / 1024.0);
        }

        private async Task FlushAllToBaseAsync(ProgressivePdfJob job, CancellationToken ct)
        {
            await job.FlushLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!job.PendingChunks.IsEmpty)
                {
                    int slabSeq = (job.PendingSlabs.IsEmpty ? 0 : job.PendingSlabs.Keys.Max()) + 1;
                    var slabPath = await FlushChunksToSlabAsync(job, slabSeq, ct).ConfigureAwait(false);
                    if (slabPath != null) job.PendingSlabs[slabSeq] = slabPath;
                }
                if (!job.PendingSlabs.IsEmpty)
                    await AccumulateSlabsToBaseAsync(job, true, ct).ConfigureAwait(false);
            }
            finally { job.FlushLock.Release(); }
        }

        // =====================================================================
        // RENDERING (jsreport)
        // =====================================================================

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
                try
                {
                    await RenderChunkToFileAsync(chunkRows, reportData, rowsKey, reportPath,
                        pageSetting, outputPath, ct, showHeader).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) when (attempt < RenderRetryAttempts)
                {
                    last = ex;
                    var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
            throw new InvalidOperationException($"Render failed after {RenderRetryAttempts} attempts.", last);
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

            var html = await _razor.RenderToStringAsync(reportPath, chunkData).ConfigureAwait(false);
            using var renderCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            renderCts.CancelAfter(RenderTimeoutMs);

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
            using var ms = new MemoryStream(capacity: 1024 * 1024);
            await result.Content.CopyToAsync(ms, renderCts.Token).ConfigureAwait(false);
            byte[] rawBytes = ms.TryGetBuffer(out var buf) ? buf.Array! : ms.ToArray();

            byte[] finalBytes = CompressChunksAfterRender
                ? await Task.Run(() => ReportUtils.CompressPdf(rawBytes, _logger), ct).ConfigureAwait(false)
                : rawBytes;

            var tmp = outputPath + ".tmp";
            await File.WriteAllBytesAsync(tmp, finalBytes, ct).ConfigureAwait(false);
            File.Move(tmp, outputPath, overwrite: true);
        }

        // =====================================================================
        // iText MERGE ENGINE
        // =====================================================================

        private async Task MergeOnMergeThreadAsync(
            IReadOnlyList<string> inputPaths,
            string outputPath,
            CompressionLevel level,
            bool stampPageNumbers,
            CancellationToken ct)
        {
            if (inputPaths.Count == 0)
                throw new ArgumentException("No inputs.", nameof(inputPaths));

            await MergeThrottle.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var unstampedPath = stampPageNumbers ? outputPath + ".unstamped" : outputPath;
                await Task.Factory.StartNew(() =>
                {
                    using var outFs = new FileStream(unstampedPath, FileMode.Create, FileAccess.Write, FileShare.None, 131072);
                    using var writer = new PdfWriter(outFs, BuildWriterProperties(level));
                    using var outDoc = new PdfDocument(writer);
                    outDoc.GetWriter().SetSmartMode(true);
                    var merger = new PdfMerger(outDoc, new PdfMergerProperties().SetCloseSrcDocuments(true));
                    foreach (var path in inputPaths)
                    {
                        if (!File.Exists(path)) continue;
                        using var reader = new PdfReader(path);
                        reader.SetUnethicalReading(true);
                        using var inDoc = new PdfDocument(reader);
                        merger.Merge(inDoc, 1, inDoc.GetNumberOfPages());
                    }
                }, ct, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);

                if (stampPageNumbers)
                {
                    await Task.Factory.StartNew(() => StampPageNumbers(unstampedPath, outputPath),
                        ct, TaskCreationOptions.LongRunning, TaskScheduler.Default).ConfigureAwait(false);
                    TryDelete(unstampedPath);
                }
            }
            finally { MergeThrottle.Release(); }
        }

        private static WriterProperties BuildWriterProperties(CompressionLevel level)
        {
            var wp = new WriterProperties();
            wp.SetFullCompressionMode(true);
            wp.SetCompressionLevel(level == CompressionLevel.Best ? CompressionConstants.BEST_COMPRESSION : CompressionConstants.BEST_SPEED);
            return wp;
        }

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
                layout.ShowTextAligned(new Paragraph($"{i} of {total}").SetFont(font).SetFontSize(10).SetFontColor(ColorConstants.BLACK),
                    size.GetWidth() / 2f, 20f, i, TextAlignment.CENTER, VerticalAlignment.BOTTOM, 0f);
            }
        }

        // =====================================================================
        // UTILITIES
        // =====================================================================

        private static List<IList<object>> PartitionRows(IList<object> source, int chunkSize)
        {
            var result = new List<IList<object>>((source.Count + chunkSize - 1) / chunkSize);
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
            try { using var r = new PdfReader(path); using var d = new PdfDocument(r); return d.GetNumberOfPages(); }
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
                long required = EstimatedCompressedBytesPerChunk * chunks * 4;
                var drive = new DriveInfo(Path.GetPathRoot(directory)!);
                if (drive.AvailableFreeSpace < required)
                    throw new IOException($"Insufficient disk space: need ~{required / 1024 / 1024} MB, have {drive.AvailableFreeSpace / 1024 / 1024} MB.");
            }
            catch (IOException) { throw; }
            catch (Exception ex) { _logger.LogWarning(ex, "⚠️ Disk space check failed – proceeding"); }
        }

        private static List<string> BuildSnapshotInputList(ProgressivePdfJob job)
        {
            var list = new List<string> { job.BasePdfPath };
            list.AddRange(job.PendingSlabs.OrderBy(kv => kv.Key).Select(kv => kv.Value).Where(File.Exists));
            list.AddRange(job.PendingChunks.OrderBy(kv => kv.Key).Select(kv => kv.Value).Where(File.Exists));
            return list;
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Static semaphores are intentionally not disposed here (process lifetime)
        }
    }
}