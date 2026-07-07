using NexgenCosysReport.Dtos.RequestDtos.Account;
using NexgenCosysReport.Dtos.RequestDtos.Account.NexgenCosysReport.Dtos.RequestDtos.AccountOperation;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IBalanceSheet
    {
        Task<BalanceSheetReportData> GetBalanceSheetReport(BalanceSheetRequest request);
    }
}
