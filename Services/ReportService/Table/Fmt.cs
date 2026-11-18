using Microsoft.AspNetCore.Html;
using System.Text.Encodings.Web;

namespace NexgenCosysReport.Services.ReportService.Table
{
    public static class Fmt
    {
        /// <summary>1,234.50   (empty when null)</summary>
        public static string Amount(decimal? value) => value?.ToString("N2") ?? "";

        /// <summary>37   (empty when null; pass `x ?? 0` to show a zero)</summary>
        public static string Int(long? value) => value?.ToString() ?? "";

        /// <summary>"label" in bold, then the value:  <b>Branch :</b> Head Office</summary>
        public static IHtmlContent Labeled(string label, string value)
        {
            var html = new HtmlContentBuilder();
            html.AppendHtml("<strong>").Append(label).AppendHtml("</strong> ").Append(value);
            return html;
        }

        public static IHtmlContent Labeled(string label, IHtmlContent value)
        {
            var html = new HtmlContentBuilder();
            html.AppendHtml("<strong>").Append(label).AppendHtml("</strong> ").AppendHtml(value);
            return html;
        }

        /// <summary>A small coloured label. Define the css class with ReportTable.Css(...).</summary>
        public static IHtmlContent Badge(string text, string cssClass)
        {
            var html = new HtmlContentBuilder();
            html.AppendHtml($"<span class=\"{HtmlEncoder.Default.Encode(cssClass)}\">").Append(text).AppendHtml("</span>");
            return html;
        }

        /// <summary>Several parts on one line, separated by spaces.</summary>
        public static IHtmlContent Join(params IHtmlContent[] parts)
        {
            var html = new HtmlContentBuilder();
            for (var i = 0; i < parts.Length; i++)
            {
                if (i > 0) html.AppendHtml("&nbsp;&nbsp;&nbsp;");
                html.AppendHtml(parts[i]);
            }
            return html;
        }
    }
}