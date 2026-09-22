// Inteface/ServiceInterface/Loan/LoanAccountWiseReport/ILoanIssueDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAccountWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.LoanAccountWiseReport
{
    public interface ILoanIssueDetailsRepository
    {
        Task<LoanIssueDetailsData> GetReportDataAsync(LoanIssueDetailsRequestDto request);
    }
}