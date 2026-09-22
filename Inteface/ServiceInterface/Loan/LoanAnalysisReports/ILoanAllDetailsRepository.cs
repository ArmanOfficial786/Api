// Inteface/ServiceInterface/Loan/LoanAnalysisReport/ILoanAllDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanAllDetailsRepository
    {
        Task<LoanAllDetailsData> GetReportDataAsync(LoanAllDetailsRequestDto request);
    }
}