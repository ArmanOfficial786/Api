using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface IFundWithdrawlRepository
    {
        Task<FundWithdrawlData> GetReportDataAsync(FundWithdrawlRequestDto request);
    }
}