// Inteface/ServiceInterface/Loan/LoanAnalysisReport/ILoanAgeingTypeWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanAgeingTypeWiseRepository
    {
        Task<LoanAgeingTypeWiseData> GetReportDataAsync(LoanAgeingTypeWiseRequestDto request);
    }
}