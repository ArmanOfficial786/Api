// Inteface/ServiceInterface/Account/OtherReports/IDailyIncomeRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IDailyIncomeRepository
    {
        Task<DailyIncomeData> GetDailyIncomeDataAsync(DailyIncomeRequestDto request);
    }
}