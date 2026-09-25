// Inteface/ServiceInterface/Microfinance/MicrofinanceReport/ICollectorWiseLoanAnalysisRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface ICollectorWiseLoanAnalysisRepository
    {
        Task<CollectorWiseLoanAnalysisData> GetReportDataAsync(CollectorWiseLoanAnalysisRequestDto request);
    }
}