// Inteface/ServiceInterface/Loan/OtherReports/ILoanFollowUpRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanFollowUpRepository
    {
        Task<LoanFollowUpData> GetReportDataAsync(LoanFollowUpRequestDto request);
    }
}