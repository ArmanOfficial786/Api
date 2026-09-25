
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanStatementRepository
    {
        Task<LoanStatementData> GetReportDataAsync(LoanStatementRequestDto request);
    }
}