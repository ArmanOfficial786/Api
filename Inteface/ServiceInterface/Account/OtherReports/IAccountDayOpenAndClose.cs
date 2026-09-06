using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IAccountDayOpenAndClose
    {
        Task<AccountDayOpenAndCloseData> GetReportDataAsync(AccountDayOpenAndCloseRequestDto request);
    }
}
