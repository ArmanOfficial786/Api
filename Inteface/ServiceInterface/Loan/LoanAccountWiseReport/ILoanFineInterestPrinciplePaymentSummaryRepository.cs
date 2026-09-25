// Inteface/ServiceInterface/Loan/OtherReports/ILoanFineInterestPrinciplePaymentSummaryRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanFineInterestPrinciplePaymentSummaryRepository
    {
        Task<LoanFineInterestPrinciplePaymentSummaryData> GetReportDataAsync(LoanFineInterestPrinciplePaymentSummaryRequestDto request);
    }
}