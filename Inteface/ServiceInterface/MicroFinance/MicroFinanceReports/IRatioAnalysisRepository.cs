// Inteface/ServiceInterface/Microfinance/MicrofinanceReport/IRatioAnalysisRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface IRatioAnalysisRepository
    {
        Task<RatioAnalysisData> GetReportDataAsync(RatioAnalysisRequestDto request);
    }
}