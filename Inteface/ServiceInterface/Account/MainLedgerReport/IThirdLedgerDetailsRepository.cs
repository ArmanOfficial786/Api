// Inteface/ServiceInterface/Account/ThirdLedgerDetailsReport/IThirdLedgerDetailsRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.ThirdLedgerDetailsReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.ThirdLedgerDetailsReport
{
    public interface IThirdLedgerDetailsRepository
    {
        Task<ThirdLedgerDetailsData> GetThirdLedgerDetailsDataAsync(ThirdLedgerDetailsRequestDto request);
    }
}