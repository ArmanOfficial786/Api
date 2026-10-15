using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IReportHeaderProvider
    {
        Task<IReadOnlyList<CommonHeader>> GetAsync(string? branchIds, CancellationToken ct = default);
    }
}
