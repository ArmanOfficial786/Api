using iText.Kernel.Pdf;
using jsreport.AspNetCore;
using jsreport.Types;
using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Utils.Report;
using System.Text;

namespace NexgenCosysReport.Services.ReportService
{
    public class JsReportService : IJsReportService
    {
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReportClient;
        private readonly IRazorRenderService _razorRenderer;
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan CacheSliding = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan CacheAbsolute = TimeSpan.FromMinutes(60);
        private const int RenderTimeoutMs = 600_000;

        public JsReportService(
            ILogger<JsReportService> logger,
            IJsReportMVCService jsReportClient,               // ← injected
            IRazorRenderService razorRenderer,
            IMemoryCache cache)
        {
            _logger = logger;
            _jsReportClient = jsReportClient;
            _razorRenderer = razorRenderer;
            _cache = cache;
        }

        // ═══════════════════════════════════════════════════════════════════
        // CACHE (unchanged)
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
        // PDF PAGE COUNT
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

        public async Task<string> ExportReportToRawHtmlAsync(
        string htmlContent,
        string? reportKey = null,
        CancellationToken ct = default)
        {
            // Use a separate key so we don't collide with the Razor HTML cache
            var viewCacheKey = reportKey != null ? $"{reportKey}_VIEW_RAW" : null;

            if (viewCacheKey != null && TryGetCachedHtml(viewCacheKey, out var cachedHtml) && cachedHtml != null)
            {
                _logger.LogInformation("📦 HTML cache hit — {Key}", viewCacheKey);
                return cachedHtml;
            }

            var request = new RenderRequest
            {
                Template = new Template
                {
                    Content = htmlContent,
                    Engine = Engine.None,
                    Recipe = Recipe.Html,
                    Helpers = null
                },
                Options = new RenderOptions { Timeout = RenderTimeoutMs }
            };

            var result = await _jsReportClient.RenderAsync(request, ct);
            using var ms = new MemoryStream();
            await result.Content.CopyToAsync(ms, ct);
            var finalHtml = Encoding.UTF8.GetString(ms.ToArray());

            if (viewCacheKey != null)
                CacheHtml(viewCacheKey, finalHtml);

            return finalHtml;
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

            JsReportTemplateHelper.ConfigureTemplate(request, format, pageSetting);

            // ✅ IReportingService exposes RenderAsync(RenderRequest)
            var result = await _jsReportClient.RenderAsync(request).ConfigureAwait(false);

            // ✅ The result is a ReportResult with Content stream
            using var ms = new MemoryStream();
            await result.Content.CopyToAsync(ms, ct).ConfigureAwait(false);
            return ms.ToArray();
        }
    }
}