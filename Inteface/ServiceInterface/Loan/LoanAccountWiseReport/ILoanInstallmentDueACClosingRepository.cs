
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanInstallmentDueACClosingRepository
    {
        Task<LoanInstallmentDueACClosingData> GetReportDataAsync(LoanInstallmentDueACClosingRequestDto request);
    }
}