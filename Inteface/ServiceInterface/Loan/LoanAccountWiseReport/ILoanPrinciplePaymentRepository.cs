// Inteface/ServiceInterface/Loan/OtherReports/ILoanPrinciplePaymentRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanPrinciplePaymentRepository
    {
        Task<LoanPrinciplePaymentData> GetReportDataAsync(LoanPrinciplePaymentRequestDto request);
    }
}