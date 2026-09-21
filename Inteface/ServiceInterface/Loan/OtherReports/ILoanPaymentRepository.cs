using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanPaymentRepository
    {
        Task<LoanPaymentData> GetReportDataAsync(LoanPaymentRequestDto request);
    }
}