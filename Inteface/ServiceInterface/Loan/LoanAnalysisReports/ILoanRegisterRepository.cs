// Inteface/ServiceInterface/Loan/LoanAnalysisReport/ILoanRegisterRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanRegisterRepository
    {
        Task<LoanRegisterData> GetReportDataAsync(LoanRegisterRequestDto request);
    }
}