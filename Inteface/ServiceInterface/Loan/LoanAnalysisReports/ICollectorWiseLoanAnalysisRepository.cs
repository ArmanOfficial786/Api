// Inteface/ServiceInterface/Loan/LoanAnalysisReport/ICollectorWiseLoanAnalysisRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ICollectorWiseLoanAnalysisRepository
    {
        Task<CollectorWiseLoanAnalysisData> GetReportDataAsync(CollectorWiseLoanAnalysisRequestDto request);
    }
}