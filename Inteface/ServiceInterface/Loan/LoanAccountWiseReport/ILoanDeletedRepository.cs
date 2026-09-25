// Inteface/ServiceInterface/Loan/LoanAccountWiseReport/ILoanDeletedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport
{
    public interface ILoanDeletedRepository
    {
        Task<LoanDeletedData> GetReportDataAsync(LoanDeletedRequestDto request);
    }
}