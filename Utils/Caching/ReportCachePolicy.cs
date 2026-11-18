namespace NexgenCosysReport.Utils.Caching
{
    /// <summary>One place for cache lifetimes. Data must not outlive the HTML built from it.</summary>
    public class ReportCachePolicy
    {
        public static readonly TimeSpan Freshness = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan HtmlSliding = TimeSpan.FromMinutes(5);
    }
}
