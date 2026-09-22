// Inteface/ServiceInterface/Loan/LoanAnalysisReport/ILoanAgeingRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanAgeingRepository
    {
        Task<LoanAgeingData> GetReportDataAsync(LoanAgeingRequestDto request);
    }
}