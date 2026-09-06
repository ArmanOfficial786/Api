// Inteface/ServiceInterface/Account/OtherReports/IAccountYearClosingRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IAccountYearClosingRepository
    {
        Task<AccountYearClosingData> GetAccountYearClosingDataAsync(AccountYearClosingRequestDto request);
    }
}