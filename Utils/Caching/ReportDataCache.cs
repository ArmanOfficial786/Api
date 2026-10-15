using Microsoft.Extensions.Caching.Memory;
using NexgenCosysReport.Inteface.ReportInterface;

namespace NexgenCosysReport.Utils.Caching
{

    /// <summary>What one DB load produces. Reused until it expires.</summary>
    public sealed record ReportDataBundle<TData, THeader>(TData Data, THeader Header);

    public sealed class ReportDataCache : IReportDataCache
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<ReportDataCache> _logger;

        public ReportDataCache(IMemoryCache cache, ILogger<ReportDataCache> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        private readonly SemaphoreSlim[] _gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();


        public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> loader, CancellationToken ct = default) where T : class
        {
            if (_cache.TryGetValue(key, out T? hit) && hit != null)
            {
                _logger.LogInformation("📦 DATA CACHE HIT  | {Key}", key);
                return hit;
            }

            var gate = _gates[(uint)key.GetHashCode() % (uint)_gates.Length];   // striped locks, no per-key leak
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_cache.TryGetValue(key, out hit) && hit != null)
                {
                    _logger.LogInformation("📦 DATA CACHE HIT (loaded by another request) | {Key}", key);
                    return hit;
                }
                _logger.LogInformation("🗄️ DATA CACHE MISS | {Key} — reading from DB", key);
                var value = await loader().ConfigureAwait(false);
                if (value != null)
                    _cache.Set(key, value, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = ReportCachePolicy.Freshness
                    });
                return value;
            }
            finally { gate.Release(); }
        }
    }
}