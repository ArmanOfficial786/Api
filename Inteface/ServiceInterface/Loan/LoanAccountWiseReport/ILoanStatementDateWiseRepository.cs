// Inteface/ServiceInterface/Loan/OtherReports/ILoanStatementDateWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanStatementDateWiseRepository
    {
        Task<LoanStatementDateWiseData> GetReportDataAsync(LoanStatementDateWiseRequestDto request);
    }
}