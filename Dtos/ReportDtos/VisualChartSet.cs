using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexgenCosysReport.Dtos.ReportDtos
{
    /// <summary>One series of a grouped bar chart: legend label, row field, colour.</summary>
    public sealed class VisualSeries
    {
        public VisualSeries(string label, string key, string color)
        {
            Label = label; Key = key; Color = color;
        }
        public string Label { get; }
        public string Key { get; }
        public string Color { get; }
    }

    /// <summary>
    /// Describes ONE chart. The generic script in _VisualCharts.cshtml draws it.
    /// Type: donut | bar | grouped | barline.   Format: compact | full | percent | int.
    /// </summary>
    public sealed class VisualChart
    {
        public required string Id { get; init; }          // id of the <canvas>
        public required string Type { get; init; }
        public string? Key { get; init; }                 // row field to plot
        public string? Color { get; init; }
        public string? LineColor { get; init; }           // barline only
        public string Format { get; init; } = "compact";  // value labels + axis ticks
        public bool Money { get; init; }                  // donut legend uses compact numbers
        public bool Horizontal { get; init; }             // bar / grouped
        public int? Thickness { get; init; }              // max bar thickness (px)
        public List<VisualSeries>? Series { get; init; }  // grouped only

        public static VisualChart Donut(string id, string key, bool money = false) =>
            new() { Id = id, Type = "donut", Key = key, Money = money };

        public static VisualChart Bar(string id, string key, string color, string format = "compact",
                                      bool horizontal = false, int? thickness = null) =>
            new() { Id = id, Type = "bar", Key = key, Color = color, Format = format, Horizontal = horizontal, Thickness = thickness };

        public static VisualChart Grouped(string id, params VisualSeries[] series) =>
            new() { Id = id, Type = "grouped", Series = series.ToList() };

        public static VisualChart BarLine(string id, string key, string barColor, string lineColor, string format = "compact") =>
            new() { Id = id, Type = "barline", Key = key, Color = barColor, LineColor = lineColor, Format = format };
    }

    /// <summary>Everything the shared partial _VisualCharts.cshtml needs.</summary>
    public sealed class VisualChartSet
    {
        public required object Rows { get; init; }                     // list of objects, one per category
        public required IReadOnlyList<VisualChart> Charts { get; init; }
        public string LabelKey { get; init; } = "name";                // row field used as the label
        public int TimeoutMs { get; init; } = 20000;                   // safety net so the PDF never hangs
        public string ChartJsUrl { get; init; } = "https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js";

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
