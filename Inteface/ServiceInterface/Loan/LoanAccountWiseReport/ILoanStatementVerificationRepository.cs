
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanStatementVerificationRepository
    {
        Task<LoanStatementVerificationData> GetReportDataAsync(LoanStatementVerificationRequestDto request);
    }
}