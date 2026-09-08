// Inteface/ServiceInterface/Account/FirstLedgerDetailsReport/IFirstLedgerDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.FirstLedgerDetailsReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.FirstLedgerDetailsReport
{
    public interface IFirstLedgerDetailsRepository
    {
        Task<FirstLedgerDetailsData> GetFirstLedgerDetailsDataAsync(FirstLedgerDetailsRequestDto request);
    }
}