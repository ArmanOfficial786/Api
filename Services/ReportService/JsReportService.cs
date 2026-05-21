using iText.Kernel.Pdf;
using jsreport.AspNetCore;
using jsreport.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Services.ReportService
{
    /// <summary>
    /// Report Service - Handles rendering Razor views to HTML and exporting to various formats
    /// 
    /// Workflow:
    /// 1. RenderRazorToHtmlAndCacheAsync() - Render Razor .cshtml → HTML → cache
    /// 2. ExportReportToFormatAsync()      - Export cached HTML to PDF/Excel/PNG
    ///
    /// Supported Export Formats:
    ///   - PDF, VIEW (renders as PDF in browser)
    ///   - HTML
    ///   - EXCEL, XLSX
    ///   - PNG
    /// </summary>
    public class JsReportService : IJsReportService
    {
        private readonly ILogger<JsReportService> _logger;
        private readonly IJsReportMVCService _jsReportMVCService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _cache;

        public JsReportService(
            ILogger<JsReportService> logger,
            IJsReportMVCService jsReportMVCService,
            IServiceProvider serviceProvider,
            IMemoryCache cache)
        {
            _logger = logger;
            _jsReportMVCService = jsReportMVCService;
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ CACHE HELPERS                                                      ║
        // ╚════════════════════════════════════════════════════════════════════╝

        public bool IsHtmlCached(string reportKey)
            => _cache.TryGetValue(reportKey, out _);

        public string? GetCachedHtml(string reportKey)
            => _cache.TryGetValue(reportKey, out string? html) ? html : null;

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ PDF PAGE COUNT                                                     ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Count pages in a rendered PDF byte array using PdfSharpCore.
        /// Called after ExportReportToFormatAsync returns PDF bytes.
        /// Cost: ~1ms — runs entirely in memory, no file I/O.
        /// </summary>
        //public static int CountPdfPages(byte[] pdfBytes)
        //{
        //    try
        //    {
        //        using var ms = new MemoryStream(pdfBytes);
        //        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.InformationOnly);
        //        return doc.PageCount;
        //    }
        //    catch
        //    {
        //        // If PDF is malformed or unreadable, fall back to 1
        //        return 1;
        //    }
        //}


        public static int CountPdfPages(byte[] pdfBytes)
        {
            try
            {
                using var ms = new MemoryStream(pdfBytes);
                using var reader = new iText.Kernel.Pdf.PdfReader(ms);
                using var doc = new PdfDocument(reader);
                return doc.GetNumberOfPages();
            }
            catch
            {
                return 1;
            }
        }


        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ RENDER PATH — Render Razor → HTML → Cache                         ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Render Razor view to HTML and cache for later exports
        /// Called once per report with DB query
        /// </summary>
        public async Task<string> RenderRazorToHtmlAndCacheAsync(
            string reportKey,
            string reportPath,
            object data)
        {
            try
            {
                // ✅ Return cached if already rendered
                if (_cache.TryGetValue(reportKey, out string? cachedHtml))
                {
                    return cachedHtml!;
                }

                // ✅ Render Razor .cshtml to HTML string
                var html = await RenderRazorViewToStringAsync(reportPath, data);

                // ✅ Cache with sliding + absolute expiration
                _cache.Set(reportKey, html, new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(20)));
                ;
                return html;
            }
            catch (Exception ex)
            {
                throw new Exception($"Render and cache failed: {ex.Message}", ex);
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ EXPORT PATH — Convert cached HTML to PDF/Excel/PNG                ║
        // ║ Called MANY times (NO DB calls — uses cache)                       ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Convert HTML to any supported format (PDF, Excel, PNG, HTML)
        /// Always called with cached HTML — NO DB CALLS
        /// </summary>
        public async Task<byte[]> ExportReportToFormatAsync(
            string htmlContent,
            string format,
            string? reportKey = null,
            PageSizeSetting? pageSetting = null)
        {
            try
            {
                var upperFormat = format.ToUpper();
                var isPdf = upperFormat == "PDF" || upperFormat == "VIEW";

                // ✅ Check cache for compressed PDF (if reportKey provided)
                if (reportKey != null && isPdf)
                {
                    var cacheKey = $"{reportKey}_{upperFormat}_compressed";
                    if (_cache.TryGetValue(cacheKey, out byte[] cachedPdf))
                    {
                        _logger.LogInformation("📦 Cache hit – returning compressed PDF for {CacheKey}", cacheKey);
                        return cachedPdf;
                    }
                }

                // ✅ Create jsreport render request
                var renderRequest = new RenderRequest
                {
                    Template = new Template
                    {
                        Content = htmlContent,
                        Engine = Engine.None,
                        Recipe = GetRecipe(upperFormat)
                    },
                    Options = new RenderOptions
                    {
                        Timeout = 600000  // 10 minutes in milliseconds
                    }
                };

                ConfigureJsReportTemplate(renderRequest, upperFormat, pageSetting);
                var result = await _jsReportMVCService.RenderAsync(renderRequest);

                using var ms = new MemoryStream();
                await result.Content.CopyToAsync(ms);
                var rawBytes = ms.ToArray();

                // 3️⃣ Compress if PDF, otherwise return raw
                byte[] finalBytes = isPdf
                    ? ReportUtils.CompressPdf(rawBytes, _logger)
                    : rawBytes;

                _logger.LogInformation("✅ Exported {Format}: {Bytes} bytes (compressed)",
                    upperFormat, finalBytes.Length);

                // 4️⃣ Store compressed PDF in cache (if reportKey provided)
                if (reportKey != null && isPdf)
                {
                    var cacheKey = $"{reportKey}_{upperFormat}_compressed";
                    _cache.Set(cacheKey, finalBytes, new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(20)));
                }
                return finalBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Export error ({Format}): {Msg}", format.ToUpper(), ex.Message);
                throw new Exception($"Export to {format} failed: {ex.Message}", ex);
            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ JSREPORT ENGINE — HTML → PDF/Excel/PNG conversions                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Map export format string to jsreport recipe enum
        /// </summary>
        private static Recipe GetRecipe(string format) => format switch
        {
            "PDF" or "VIEW" => Recipe.ChromePdf,
            "HTML" => Recipe.Html,
            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
            "WORD" or "DOCX" => Recipe.HtmlEmbeddedInDocx,
            "PNG" => Recipe.ChromeImage,
            _ => Recipe.ChromePdf
        };

        /// <summary>
        /// Apply format-specific options (margins, page size, etc.) to jsreport request
        /// </summary>
        private static void ConfigureJsReportTemplate(RenderRequest request, string format, PageSizeSetting? pageSetting = null)
        {
            switch (format)
            {
                case "PDF":
                case "VIEW":
                    var opts = pageSetting ?? new PageSizeSetting();

                    var chrome = new Chrome
                    {

                        MarginTop = opts.MarginTop,
                        MarginBottom = opts.MarginBottom,
                        MarginLeft = opts.MarginLeft,
                        MarginRight = opts.MarginRight,
                        WaitForJS = false,
                        WaitForNetworkIddle = true,
                        DisplayHeaderFooter = true,
                        PrintBackground = false,
                        Landscape = opts.Landscape,

                        // ── Footer template with page numbering ──────────────────
                        HeaderTemplate = "<span></span>",
                        FooterTemplate = @"
                        <div style='
                                font-size: 5pt;
                                width: 100%; 
                                text-align: center;
                                border-top: 1px solid #ccc;
                                line-height: 20px;
                                padding:0; margin: 0;'>
                               <span class='pageNumber'></span> of <span class='totalPages'></span>
                         </div>
                        ",
                    };

                    if (opts.ResolvedFormat != null)
                        chrome.Format = opts.ResolvedFormat;      // named format: A4, A3…
                    else
                    {
                        chrome.Width = opts.ResolvedWidth;       // custom: "380mm"
                        chrome.Height = opts.ResolvedHeight;      // custom: "210mm"
                    }

                    request.Template.Chrome = chrome;



                    //request.Template.Chrome = new Chrome
                    //{
                    //    MarginTop = "1mm",
                    //    MarginBottom = "1mm",
                    //    MarginLeft = "5mm",
                    //    MarginRight = "5mm",
                    //    DisplayHeaderFooter = false,
                    //    PrintBackground = true,
                    //    Format = "A4",
                    //    Landscape = false
                    //};
                    break;

                case "EXCEL":
                case "XLSX":
                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
                    break;

            }
        }

        // ╔════════════════════════════════════════════════════════════════════╗
        // ║ INTERNAL HELPERS — Razor rendering                                ║
        // ╚════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Render Razor .cshtml file to HTML string
        /// </summary>
        private async Task<string> RenderRazorViewToStringAsync(string viewName, object model)
        {
            try
            {
                // ✅ Create scope to avoid captive dependency issues
                using var scope = _serviceProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;

                var httpContext = new DefaultHttpContext { RequestServices = scopedProvider };
                var actionContext = new ActionContext(
                    httpContext,
                    new RouteData(),
                    new ActionDescriptor());

                var viewEngine = scopedProvider.GetRequiredService<IRazorViewEngine>();
                var tempDataProvider = scopedProvider.GetRequiredService<ITempDataProvider>();

                // ✅ Try absolute path first, then by name
                var viewResult = viewEngine.GetView(null, viewName, false);
                if (!viewResult.Success)
                    viewResult = viewEngine.FindView(actionContext, viewName, false);

                if (!viewResult.Success)
                    throw new InvalidOperationException($"View '{viewName}' not found.");

                await using var sw = new StringWriter();

                var viewData = new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary())
                { Model = model };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewData,
                    new TempDataDictionary(httpContext, tempDataProvider),
                    sw,
                    new HtmlHelperOptions());

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RenderRazorViewToStringAsync failed: {Msg}", ex.Message);
                throw;
            }
        }
    }
}
