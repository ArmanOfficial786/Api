
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanCICRepository
    {
        Task<LoanCICData> GetReportDataAsync(LoanCICRequestDto request);
    }
}