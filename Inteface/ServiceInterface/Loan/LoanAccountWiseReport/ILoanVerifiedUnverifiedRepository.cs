
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport
{
    public interface ILoanVerifiedUnverifiedRepository
    {
        Task<LoanVerifiedUnverifiedData> GetReportDataAsync(LoanVerifiedUnverifiedRequestDto request);
    }
}