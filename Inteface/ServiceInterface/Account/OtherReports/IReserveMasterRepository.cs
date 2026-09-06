// Inteface/ServiceInterface/Account/OtherReports/IReserveMasterRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IReserveMasterRepository
    {
        Task<ReserveMasterData> GetReserveMasterDataAsync(ReserveMasterRequestDto request);
    }
}