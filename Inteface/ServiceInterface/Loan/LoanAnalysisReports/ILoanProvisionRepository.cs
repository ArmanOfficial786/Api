
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAnalysisReport
{
    public interface ILoanProvisionRepository
    {
        Task<LoanProvisionData> GetReportDataAsync(LoanProvisionRequestDto request);
    }
}