//using NexgenCosysReport.Dtos.ReportDtos;

//namespace NexgenCosysReport.Inteface.ReportInterface
//{
//    public interface IProgressivePdfService
//    {
//        Task<ProgressivePdfJob> StartAsync(
//            string reportPath,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            int firstChunkSize,
//            int subsequentChunkSize,
//            PageSizeSetting? pageSetting,
//            CancellationToken ct);

//        ProgressivePdfJob? GetJob(string jobId);
//    }
//}






//// IProgressivePdfService.cs
//using NexgenCosysReport.Dtos.ReportDtos;

//namespace NexgenCosysReport.Inteface.ReportInterface
//{
//    public interface IProgressivePdfService
//    {
//        /// <summary>
//        /// Starts a progressive PDF job.  Returns after the first chunk is
//        /// ready.  All subsequent chunks render in the background.
//        /// </summary>
//        /// <param name="targetChunkBytes">
//        /// Desired compressed size per subsequent chunk (default 3 MB).
//        /// The service auto-calibrates row count after the first chunk.
//        /// </param>
//        Task<ProgressivePdfJob> StartAsync(
//            string reportPath,
//            IDictionary<string, object> reportData,
//            string rowsKey,
//            PageSizeSetting? pageSetting,
//            CancellationToken ct,
//            int chunkSize = 500,           // matches implementation default
//            int maxParallelism = 3);

//        /// <summary>Retrieves a live job from cache; null if expired or not found.</summary>
//        ProgressivePdfJob? GetJob(string jobId);

//        /// <summary>
//        /// Stamps base.pdf into a temporary snapshot, reads its bytes, deletes
//        /// the snapshot, and returns the bytes together with the job state.
//        /// Thread-safe — acquires FileLock internally.
//        /// </summary>
//        Task<(byte[] Bytes, ProgressivePdfJob Job)> GetSnapshotAsync(
//            string jobId, CancellationToken ct);
//    }
//}



//best for all

using NexgenCosysReport.Dtos.ReportDtos;

namespace NexgenCosysReport.Inteface.ReportInterface
{
    public interface IProgressivePdfService
    {
        Task<ProgressivePdfJob> StartAsync(
            string reportPath,
            IDictionary<string, object> reportData,
            string rowsKey,
            PageSizeSetting? pageSetting,
            CancellationToken ct,
            int chunkSize = 500,
            int maxParallelism = 4);

        ProgressivePdfJob? GetJob(string jobId);

        Task<(byte[] Bytes, ProgressivePdfJob Job)> GetSnapshotAsync(
            string jobId, CancellationToken ct);

        Task<(byte[] Bytes, ProgressivePdfJob Job)> GetFinalAsync(
            string jobId, CancellationToken ct);
    }
}