using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IRatioAnalysis
    {
        Task<RatioAnalysisData> GetRatioAnalysis(RatioAnalysisRequest request);
    }
}
