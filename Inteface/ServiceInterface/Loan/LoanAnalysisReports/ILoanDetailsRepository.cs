
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanDetailsRepository
    {
        Task<LoanDetailsData> GetReportDataAsync(LoanDetailsRequestDto request);
    }
}