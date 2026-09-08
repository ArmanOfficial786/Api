// Inteface/ServiceInterface/AccountOperation/OthersReport/ICashAndBankBalanceRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport
{
    public interface ICashAndBankBalanceRepository
    {
        Task<CashAndBankBalanceData> GetReportDataAsync(CashAndBankBalanceRequestDto request);
    }
}