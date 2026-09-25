using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface ILoanSummaryRepository
    {
        Task<LoanSummaryData> GetReportDataAsync(LoanSummaryRequestDto request);
    }
}