using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using jsreport.AspNetCore;
using jsreport.Types;
using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
namespace NexgenCosysReport.Services.ReportService
{
    public class PdfChunkService : IPdfChunkService
    {
        private readonly IRazorRenderService _razorRenderer;
        private readonly IJsReportMVCService _jsReport;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PdfChunkService> _logger;

        private const int RenderTimeoutMs = 600_000;
        private const long PdfCacheMaxBytes = 50L * 1024 * 1024; // 50 MB

        public PdfChunkService(
            IRazorRenderService razorRenderer,
            IJsReportMVCService jsReport,
            IMemoryCache cache,
            ILogger<PdfChunkService> logger)
        {
            _razorRenderer = razorRenderer;
            _jsReport = jsReport;
            _cache = cache;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════════

        public bool TryGetChunkedPdfPath(string reportKey, out string? pdfPath)
        {
            if (_cache.TryGetValue(CacheKey(reportKey), out pdfPath) &&
                !string.IsNullOrEmpty(pdfPath) &&
                File.Exists(pdfPath))          // guard: file may have been deleted
                return true;

            pdfPath = null;
            return false;
        }

        public async Task<string> ExportChunkedPdfAsync(
            string reportKey,
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            int chunkSize,
            PageSizeSetting? pageSetting = null,
            int maxParallelism = 2,
            CancellationToken ct = default)
        {
            if (!reportData.TryGetValue(rowsKey, out var rowsObj) ||
                rowsObj is not IEnumerable<object> rowsEnum)
                throw new ArgumentException($"reportData['{rowsKey}'] must be IEnumerable<object>.");

            var allRows = rowsEnum as IList<object> ?? rowsEnum.ToList();
            var chunks = SplitIntoChunks(allRows, chunkSize);
            var chunkFiles = new string[chunks.Count];

            _logger.LogInformation("🔀 {Total} rows → {Count} chunks × {Size} (parallel={P})",
                allRows.Count, chunks.Count, chunkSize, maxParallelism);

            var tempDir = Path.Combine(Path.GetTempPath(), "nexgen_pdf", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                // ── Render chunks in parallel (bounded) ───────────────────
                using var sem = new SemaphoreSlim(maxParallelism);
                var tasks = new List<Task>(chunks.Count);

                for (int i = 0; i < chunks.Count; i++)
                {
                    int idx = i;
                    await sem.WaitAsync(ct).ConfigureAwait(false);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var chunkData = new Dictionary<string, object>(reportData)
                            {
                                [rowsKey] = chunks[idx]
                            };

                            var html = await _razorRenderer
                                .RenderToStringAsync(reportPath, chunkData)
                                .ConfigureAwait(false);

                            var chunkFile = Path.Combine(tempDir, $"chunk_{idx:D5}.pdf");

                            await RenderHtmlToFileAsync(html, pageSetting, chunkFile, ct)
                                .ConfigureAwait(false);

                            chunkFiles[idx] = chunkFile;
                            chunks[idx] = null!;   // release memory ASAP

                            _logger.LogInformation("✅ Chunk {Idx}/{Total} — {MB:F2} MB",
                                idx + 1, chunks.Count,
                                new FileInfo(chunkFile).Length / 1024.0 / 1024.0);
                        }
                        finally { sem.Release(); }
                    }, ct));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                chunks.Clear();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // ── Merge ─────────────────────────────────────────────────
                var mergedFile = Path.Combine(tempDir, "merged.pdf");
                MergePdfFiles(chunkFiles, mergedFile);

                // ── Stamp global page numbers (no jsreport footer in chunks)
                StampPageNumbers(mergedFile);

                var mergedSize = new FileInfo(mergedFile).Length;
                _logger.LogInformation("🔗 Merged PDF: {MB:F2} MB", mergedSize / 1024.0 / 1024.0);

                // ── Cache path (with eviction cleanup) ────────────────────
                CacheChunkedPdfPath(reportKey, mergedFile);

                // ── Delete individual chunk files ─────────────────────────
                foreach (var f in chunkFiles) TryDelete(f);

                return mergedFile;
            }
            catch
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Streams jsreport output directly to disk — no byte[] in memory.</summary>
        private async Task RenderHtmlToFileAsync(
            string html, PageSizeSetting? pageSetting, string outputPath, CancellationToken ct)
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

            // suppressFooter=true — StampPageNumbers adds the correct global numbers
            JsReportTemplateHelper.ConfigureTemplate(request, "PDF", pageSetting, suppressFooter: true);

            var result = await _jsReport.RenderAsync(request).ConfigureAwait(false);

            await using var fs = new FileStream(
                outputPath, FileMode.Create, FileAccess.Write, FileShare.None,
                bufferSize: 81_920, useAsync: true);

            await result.Content.CopyToAsync(fs, ct).ConfigureAwait(false);
        }

        private static void MergePdfFiles(string[] inputFiles, string outputFile)
        {
            using var outFs = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var writer = new PdfWriter(outFs);
            using var outDoc = new PdfDocument(writer);
            var merger = new iText.Kernel.Utils.PdfMerger(outDoc);

            foreach (var file in inputFiles)
            {
                if (string.IsNullOrEmpty(file) || !File.Exists(file)) continue;
                using var inFs = new FileStream(file, FileMode.Open, FileAccess.Read);
                using var reader = new PdfReader(inFs);
                using var inDoc = new PdfDocument(reader);
                merger.Merge(inDoc, 1, inDoc.GetNumberOfPages());
            }
            outDoc.Close(); // flush iText buffers before Dispose
        }

        /// <summary>
        /// Stamps "N of Total" on every page of the merged PDF.
        /// Works on a temp file to avoid iText locking conflicts, then replaces the original.
        /// </summary>
        private static void StampPageNumbers(string inputPdfPath)
        {
            var tempPath = Path.Combine(
                Path.GetDirectoryName(inputPdfPath) ?? ".",
                Path.GetFileNameWithoutExtension(inputPdfPath) + "_stamped.pdf");

            using (var reader = new PdfReader(inputPdfPath))
            using (var writer = new PdfWriter(tempPath))
            using (var doc = new PdfDocument(reader, writer))
            {
                int totalPages = doc.GetNumberOfPages();
                var font = PdfFontFactory.CreateFont();

                for (int i = 1; i <= totalPages; i++)
                {
                    var page = doc.GetPage(i);
                    var rect = page.GetPageSize();
                    var canvas = new PdfCanvas(page.NewContentStreamBefore(), page.GetResources(), doc);

                    float x = (rect.GetLeft() + rect.GetRight()) / 2f;
                    float y = rect.GetBottom() + 15f;

                    canvas.BeginText()
                          .SetFontAndSize(font, 10)
                          .MoveText(x, y)
                          .ShowText($"{i} of {totalPages}")
                          .EndText();
                }
            }

            File.Delete(inputPdfPath);
            File.Move(tempPath, inputPdfPath);
        }

        private void CacheChunkedPdfPath(string reportKey, string path)
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(30),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
            };

            // Delete the temp file when the cache entry is evicted
            options.RegisterPostEvictionCallback((_, value, _, _) =>
            {
                if (value is not string filePath) return;
                TryDelete(filePath);
                try
                {
                    var dir = Path.GetDirectoryName(filePath);
                    if (dir != null && Directory.Exists(dir) &&
                        !Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch { /* best-effort */ }
            });

            _cache.Set(CacheKey(reportKey), path, options);
        }

        private static string CacheKey(string reportKey) => $"{reportKey}_chunked_path";

        private static void TryDelete(string? path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); }
            catch { /* ignore */ }
        }

        private static List<IList<object>> SplitIntoChunks(IList<object> source, int chunkSize)
        {
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

            var total = source.Count;
            var chunks = new List<IList<object>>((total + chunkSize - 1) / chunkSize);

            for (int start = 0; start < total; start += chunkSize)
            {
                var size = Math.Min(chunkSize, total - start);
                var chunk = new List<object>(size);
                for (int i = 0; i < size; i++)
                    chunk.Add(source[start + i]);
                chunks.Add(chunk);
            }
            return chunks;
        }
    }
}
