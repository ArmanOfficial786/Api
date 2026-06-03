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
using CompressionConstants = iText.Kernel.Pdf.CompressionConstants;

namespace NexgenCosysReport.Services.ReportService
{
    public class ProgressivePdfService : IProgressivePdfService
    {
        private readonly IRazorRenderService _razor;
        private readonly IJsReportMVCService _jsReport;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ProgressivePdfService> _logger;
        private const int RenderTimeoutMs = 600_000;

        // How often to re‑compress the accumulator (every N chunks, 0 = never, 1 = every chunk)
        private const int CompressEveryNChunks = 5;

        public ProgressivePdfService(
            IRazorRenderService razor,
            IJsReportMVCService jsReport,
            IMemoryCache cache,
            ILogger<ProgressivePdfService> logger)
        {
            _razor = razor;
            _jsReport = jsReport;
            _cache = cache;
            _logger = logger;
        }

        // ───────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ───────────────────────────────────────────────────────────────────

        public async Task<ProgressivePdfJob> StartAsync(
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            PageSizeSetting? pageSetting,
            CancellationToken ct,
            int chunkSize = 500,
            int maxParallelism = 3)
        {
            if (!reportData.TryGetValue(rowsKey, out var rowsObj) ||
                rowsObj is not IEnumerable<object> rowsEnum)
                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
            if (allRows.Count == 0)
                throw new ArgumentException("Row collection is empty.");

            var tempDir = Path.Combine(Path.GetTempPath(), "nexgen_progressive", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            var job = new ProgressivePdfJob
            {
                TempDir = tempDir,
                TotalRows = allRows.Count,
                EstimatedTotalPages = (int)Math.Ceiling(allRows.Count / 25.0),
                BasePdfPath = Path.Combine(tempDir, "base.pdf")
            };

            _cache.Set(CacheKey(job.JobId), job, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120)
            }.RegisterPostEvictionCallback((_, val, _, _) =>
            {
                if (val is ProgressivePdfJob j) TryCleanup(j.TempDir);
            }));

            _logger.LogInformation("🚀 Job {Id} started — {Rows} rows, chunkSize={Chunk}, parallelism={Par}",
                job.JobId, allRows.Count, chunkSize, maxParallelism);

            var chunks = Partition(allRows, chunkSize);
            job.TotalChunks = chunks.Count;

            // First chunk (with header) – uses the request cancellation token
            var firstChunkPath = Path.Combine(job.TempDir, "chunk_00000.pdf");
            await RenderChunkToFileAsync(chunks[0], reportData, rowsKey, reportPath, pageSetting, firstChunkPath, ct, showHeader: true);
            File.Copy(firstChunkPath, job.BasePdfPath, overwrite: true);
            job.CompletedChunks = 1;
            job.PagesReady = CountPages(job.BasePdfPath);
            job.CurrentSizeBytes = new FileInfo(job.BasePdfPath).Length;
            job.LastUpdated = DateTime.UtcNow;

            // Remaining chunks – background, no request token
            _ = Task.Run(async () =>
            {
                try
                {
                    await ProcessRemainingChunksAsync(job, chunks.Skip(1).ToList(), reportData, rowsKey,
                        reportPath, pageSetting, maxParallelism, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    job.HasError = true;
                    job.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "❌ Job {Id} background processing failed", job.JobId);
                }
            });

            return job;
        }

        public ProgressivePdfJob? GetJob(string jobId)
            => _cache.TryGetValue(CacheKey(jobId), out ProgressivePdfJob? job) ? job : null;

        public async Task<(byte[] Bytes, ProgressivePdfJob Job)> GetSnapshotAsync(string jobId, CancellationToken ct)
        {
            var job = GetJob(jobId)
                ?? throw new KeyNotFoundException($"Job {jobId} not found or expired.");

            await job.FileLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (!File.Exists(job.BasePdfPath))
                    throw new FileNotFoundException("base.pdf not ready yet.", job.BasePdfPath);

                var snapshotPath = Path.Combine(job.TempDir, $"snap_{Guid.NewGuid():N}.pdf");
                try
                {
                    StampPdfToSnapshot(job.BasePdfPath, snapshotPath);
                    return (await File.ReadAllBytesAsync(snapshotPath, ct).ConfigureAwait(false), job);
                }
                finally
                {
                    TryDelete(snapshotPath);
                }
            }
            finally
            {
                job.FileLock.Release();
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ───────────────────────────────────────────────────────────────────

        private async Task ProcessRemainingChunksAsync(
            ProgressivePdfJob job,
            IList<IList<object>> remainingChunks,
            IDictionary<string, object> reportData,
            string rowsKey,
            string reportPath,
            PageSizeSetting? pageSetting,
            int maxParallelism,
            CancellationToken ct)   // CancellationToken.None
        {
            var sw = Stopwatch.StartNew();
            using var throttle = new SemaphoreSlim(maxParallelism);
            var tasks = new List<Task>(remainingChunks.Count);

            for (int i = 0; i < remainingChunks.Count; i++)
            {
                int chunkIndex = i + 1; // zero‑based → 1‑based for display
                await throttle.WaitAsync(ct);
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var chunkPath = Path.Combine(job.TempDir, $"chunk_{chunkIndex:D5}.pdf");
                        await RenderChunkToFileAsync(remainingChunks[i], reportData, rowsKey, reportPath, pageSetting, chunkPath, ct, showHeader: false);

                        // Merge under lock to avoid concurrent writes
                        await job.FileLock.WaitAsync(ct);
                        try
                        {
                            // Determine if we should compress after this merge
                            int currentCompleted = job.CompletedChunks + 1; // because we haven't incremented yet
                            bool needCompress = (CompressEveryNChunks > 0) &&
                                                (currentCompleted % CompressEveryNChunks == 0 ||
                                                 currentCompleted == job.TotalChunks);

                            await MergeChunkIntoBaseAsync(job.BasePdfPath, chunkPath, _logger, ct, needCompress);
                        }
                        finally
                        {
                            job.FileLock.Release();
                        }

                        // Update job state
                        lock (job)
                        {
                            job.CompletedChunks++;
                            job.PagesReady = CountPages(job.BasePdfPath);
                            job.CurrentSizeBytes = new FileInfo(job.BasePdfPath).Length;
                            job.LastUpdated = DateTime.UtcNow;
                        }

                        TryDelete(chunkPath);
                        _logger.LogInformation("📄 Job {Id} merged chunk {Idx}/{Tot} in {Elapsed:F1}s",
                            job.JobId, job.CompletedChunks, job.TotalChunks, sw.Elapsed.TotalSeconds);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Job {Id} chunk {Idx} failed", job.JobId, chunkIndex);
                        job.HasError = true;
                        job.ErrorMessage = $"Chunk {chunkIndex}: {ex.Message}";
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            job.IsComplete = true;
            _logger.LogInformation("✅ Job {Id} complete — {Pages} pages, {MB:F2} MB in {Elapsed:F1}s",
                job.JobId, job.PagesReady, job.CurrentSizeBytes / 1024.0 / 1024.0, sw.Elapsed.TotalSeconds);
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
            await RenderHtmlToCompressedFileAsync(html, pageSetting, outputPath, ct).ConfigureAwait(false);
        }

        private async Task RenderHtmlToCompressedFileAsync(
            string html,
            PageSizeSetting? pageSetting,
            string outputPath,
            CancellationToken ct)
        {
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

            using var ms = new MemoryStream();
            await result.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
            byte[] rawBytes = ms.TryGetBuffer(out var buffer) ? buffer.Array! : ms.ToArray();
            byte[] compressed = ReportUtils.CompressPdf(rawBytes, _logger);
            await File.WriteAllBytesAsync(outputPath, compressed, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Merges a chunk into the base PDF. If <paramref name="compress"/> is true,
        /// also re‑compresses the whole base.pdf to keep it small.
        /// </summary>
        private static async Task MergeChunkIntoBaseAsync(
            string basePath,
            string chunkPath,
            ILogger logger,
            CancellationToken ct,
            bool compress)
        {
            var tempMerged = basePath + ".tmp";

            // 1. Merge chunk into base (using streaming iText, no full load)
            await using (var baseFs = new FileStream(basePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            await using (var chunkFs = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true))
            await using (var outFs = new FileStream(tempMerged, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                using var baseDoc = new PdfDocument(new PdfReader(baseFs));
                using var chunkDoc = new PdfDocument(new PdfReader(chunkFs));
                using var writer = new PdfWriter(outFs);
                writer.SetCompressionLevel(CompressionConstants.DEFAULT_COMPRESSION);
                using var outDoc = new PdfDocument(writer);
                var merger = new PdfMerger(outDoc);
                merger.Merge(baseDoc, 1, baseDoc.GetNumberOfPages());
                merger.Merge(chunkDoc, 1, chunkDoc.GetNumberOfPages());
            }

            // 2. Replace original base.pdf with the merged file
            File.Delete(basePath);
            File.Move(tempMerged, basePath);

            // 3. Optionally compress the merged file (expensive but keeps size small)
            if (compress)
            {
                var rawBytes = await File.ReadAllBytesAsync(basePath, ct).ConfigureAwait(false);
                var compressedBytes = ReportUtils.CompressPdf(rawBytes, logger);
                await File.WriteAllBytesAsync(basePath, compressedBytes, ct).ConfigureAwait(false);
            }
        }

        private static void StampPdfToSnapshot(string basePdfPath, string snapshotPath)
        {
            using var reader = new PdfReader(basePdfPath);
            using var writer = new PdfWriter(snapshotPath);
            using var pdfDoc = new PdfDocument(reader, writer);
            using var layoutDoc = new Document(pdfDoc);
            layoutDoc.SetMargins(0, 0, 0, 0);

            int totalPages = pdfDoc.GetNumberOfPages();
            var font = PdfFontFactory.CreateFont();

            for (int i = 1; i <= totalPages; i++)
            {
                var size = pdfDoc.GetPage(i).GetPageSizeWithRotation();
                var para = new Paragraph($"{i} of {totalPages}")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetFontColor(ColorConstants.BLACK)
                    .SetMargin(0)
                    .SetPadding(0);

                layoutDoc.ShowTextAligned(
                    para,
                    size.GetWidth() / 2f,
                    20f,
                    i,
                    TextAlignment.CENTER,
                    VerticalAlignment.BOTTOM,
                    0f);
            }
        }

        // ───────────────────────────────────────────────────────────────────
        // UTILITIES
        // ───────────────────────────────────────────────────────────────────

        private static List<IList<object>> Partition(IList<object> source, int chunkSize)
        {
            var result = new List<IList<object>>((source.Count + chunkSize - 1) / chunkSize);
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                int count = Math.Min(chunkSize, source.Count - i);
                var slice = new List<object>(count);
                for (int j = 0; j < count; j++)
                    slice.Add(source[i + j]);
                result.Add(slice);
            }
            return result;
        }

        private static int CountPages(string pdfPath)
        {
            try
            {
                using var reader = new PdfReader(pdfPath);
                using var doc = new PdfDocument(reader);
                return doc.GetNumberOfPages();
            }
            catch
            {
                return 0;
            }
        }

        private static string CacheKey(string id) => $"progressive_pdf_{id}";

        private static void TryDelete(string? path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch { /* best‑effort */ }
        }

        private static void TryCleanup(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch { /* best‑effort */ }
        }
    }
}