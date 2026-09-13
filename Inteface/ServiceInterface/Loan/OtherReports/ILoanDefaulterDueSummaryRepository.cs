// Inteface/ServiceInterface/Loan/OtherReports/ILoanDefaulterDueSummaryRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanDefaulterDueSummaryRepository
    {
        Task<LoanDefaulterDueSummaryData> GetReportDataAsync(LoanDefaulterDueSummaryRequestDto request);
    }
}