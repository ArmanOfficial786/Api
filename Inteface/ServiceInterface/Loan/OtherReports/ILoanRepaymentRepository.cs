// Inteface/ServiceInterface/Loan/OtherReports/ILoanRepaymentRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanRepaymentRepository
    {
        Task<LoanRepaymentData> GetReportDataAsync(LoanRepaymentRequestDto request);
    }
}