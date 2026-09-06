using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IPEARLSAnalysis
    {
        Task<PEARLSAnalysisData> GetPEARLSAnalysisDataAsync(PEARLSAnalysisRequestDto request);
    }
}
