namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IReportDataCache
    {
        /// <summary>Cached value, or runs the loader once even for concurrent callers. Null is not cached.</summary>
        Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> loader, CancellationToken ct = default) where T : class;
    }
}
