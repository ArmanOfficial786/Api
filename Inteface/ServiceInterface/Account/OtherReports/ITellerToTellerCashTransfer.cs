using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface ITellerToTellerCashTransfer
    {
        Task<TellerToTellerCashTransferData> GetReportDataAsync(TellerToTellerCashTransferRequestDto request);
    }
}
