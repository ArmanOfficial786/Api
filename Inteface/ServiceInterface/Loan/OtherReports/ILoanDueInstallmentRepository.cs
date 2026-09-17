using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanDueInstallmentRepository
    {
        Task<LoanDueInstallmentData> GetReportDataAsync(LoanDueInstallmentRequestDto request);
    }
}