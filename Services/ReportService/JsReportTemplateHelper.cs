using jsreport.Types;
using NexgenCosysReport.Dtos.ReportDtos;
namespace NexgenCosysReport.Services.ReportService
{
    internal static class JsReportTemplateHelper
    {
        internal const string FooterHtml = """
            <div style='font-size:10pt;width:100%;text-align:center;
                        border-top:1px solid #ccc;line-height:20px;'>
                <span class='pageNumber'></span> of <span class='totalPages'></span>
            </div>
            """;

        internal static Recipe GetRecipe(string format) => format switch
        {
            "PDF" or "VIEW" => Recipe.ChromePdf,
            "HTML" => Recipe.Html,
            "EXCEL" or "XLSX" => Recipe.HtmlToXlsx,
            "WORD" or "DOCX" => Recipe.HtmlEmbeddedInDocx,
            "PNG" => Recipe.ChromeImage,
            _ => Recipe.ChromePdf
        };

        internal static void ConfigureTemplate(
            RenderRequest request,
            string format,
            PageSizeSetting? pageSetting,
            bool suppressFooter = false,
            (double W, double H)? sheet = null)
        {
            switch (format)
            {
                case "PDF":
                case "VIEW":
                    // Visual report: the HTML defines the pages. Exact size, zero margins, no Chrome footer.
                    if (sheet is { } sz)
                    {
                        request.Template.Chrome = new Chrome
                        {
                            Width = SheetLayout.Mm(sz.W),
                            Height = SheetLayout.Mm(sz.H),
                            Landscape = false,            // orientation is already baked into width/height
                            MarginTop = "0",
                            MarginBottom = "0",
                            MarginLeft = "0",
                            MarginRight = "0",
                            DisplayHeaderFooter = false,
                            PrintBackground = true,
                            WaitForJS = true,             // waits for window.JSREPORT_READY_TO_START
                            WaitForNetworkIddle = true
                        };
                        break;
                    }
                    //for normal report
                    var opts = pageSetting ?? new PageSizeSetting();
                    var chrome = new Chrome
                    {
                        MarginTop = opts.MarginTop,
                        MarginBottom = opts.MarginBottom,
                        MarginLeft = opts.MarginLeft,
                        MarginRight = opts.MarginRight,
                        WaitForJS = false,
                        WaitForNetworkIddle = true,
                        PrintBackground = false,
                        Landscape = opts.Landscape,
                        DisplayHeaderFooter = !suppressFooter,
                        HeaderTemplate = suppressFooter ? null : "<span></span>",
                        FooterTemplate = suppressFooter ? null : FooterHtml
                    };

                    if (opts.ResolvedFormat != null)
                        chrome.Format = opts.ResolvedFormat;
                    else
                    {
                        chrome.Width = opts.ResolvedWidth;
                        chrome.Height = opts.ResolvedHeight;
                    }
                    request.Template.Chrome = chrome;
                    break;
                case "HTML":

                    // intentional no-op
                    break;


                case "EXCEL":
                case "XLSX":
                    request.Template.HtmlToXlsx = new HtmlToXlsx { HtmlEngine = "chrome" };
                    break;


            }
        }
    }
}
