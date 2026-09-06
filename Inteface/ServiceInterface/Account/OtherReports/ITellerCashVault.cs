using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface ITellerCashVault
    {
        Task<TellerCashVaultData> GetReportDataAsync(TellerCashVaultRequestDto request);
    }
}
