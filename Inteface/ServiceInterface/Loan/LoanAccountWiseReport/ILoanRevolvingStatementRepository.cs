// Inteface/ServiceInterface/Loan/OtherReports/ILoanRevolvingStatementRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanRevolvingStatementRepository
    {
        Task<LoanRevolvingStatementData> GetReportDataAsync(LoanRevolvingStatementRequestDto request);
    }
}