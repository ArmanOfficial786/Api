using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IProgressivePdfService
    {
        Task<ProgressivePdfJob> StartAsync(
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            int firstChunkSize,
            int subsequentChunkSize,
            PageSizeSetting? pageSetting,
            CancellationToken ct);

        ProgressivePdfJob? GetJob(string jobId);
    }
}
