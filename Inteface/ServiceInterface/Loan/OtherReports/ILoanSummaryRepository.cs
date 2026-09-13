// Inteface/ServiceInterface/Loan/OtherReports/ILoanSummaryRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface ILoanSummaryRepository
    {
        Task<LoanSummaryData> GetReportDataAsync(LoanSummaryRequestDto request);
    }
}