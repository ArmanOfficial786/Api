using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IRatioAnalysis
    {
        Task<RatioAnalysisData> GetRatioAnalysis(RatioAnalysisRequest request);
    }
}
