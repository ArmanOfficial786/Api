// Utils/Caching/ReportHeaderProvider.cs
using Microsoft.Extensions.Options;
using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Inteface.ReportInterface;
using NexgenCosysReport.Inteface.ServiceInterface.Common;
using NexgenCosysReport.Utils.Report;

namespace NexgenCosysReport.Utils.Caching
{
    //public interface IReportHeaderProvider
    //{
    //    /// <summary>Common header (company name, address, logo) for the branch. Built once per branch, then cached.</summary>
    //    Task<IReadOnlyList<CommonHeader>> GetAsync(string? branchIds, CancellationToken ct = default);
    //}

    public sealed class ReportHeaderProvider : IReportHeaderProvider
    {
        private readonly ICommonHeaderRepository _repository;
        private readonly IReportDataCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IOptions<ReportSettings> _settings;

        public ReportHeaderProvider(
            ICommonHeaderRepository repository,
            IReportDataCache cache,
            IWebHostEnvironment env,
            IOptions<ReportSettings> settings)
        {
            _repository = repository;
            _cache = cache;
            _env = env;
            _settings = settings;
        }

        public async Task<IReadOnlyList<CommonHeader>> GetAsync(string? branchIds, CancellationToken ct = default)
        {
            // one branch selected -> that branch's header; none, "-1" or several -> the default (all) header
            var branchId = string.IsNullOrWhiteSpace(branchIds) || branchIds == "-1" || branchIds.Contains(',')
                ? null
                : branchIds;

            var headers = await _cache.GetOrLoadAsync<List<CommonHeader>>($"Header_{branchId ?? "ALL"}", async () =>
            {
                var loaded = (await _repository.GetCommonHeaders(branchId ?? "")).ToList();

                // runs once, on the freshly loaded objects, before they are cached
                var webRoot = ReportUtils.GetWebRootPath(_env, _settings);
                await ReportUtils.ConvertUniqueImagesToBase64Async(
                    loaded, nameof(CommonHeader.CompanyLogo), webRoot);

                return loaded.Count > 0 ? loaded : null;        // an empty header is never cached
            }, ct);

            return headers ?? new List<CommonHeader>();
        }
    }
}