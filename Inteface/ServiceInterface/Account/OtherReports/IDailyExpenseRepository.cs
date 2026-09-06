// Inteface/ServiceInterface/Account/OtherReports/IDailyExpenseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IDailyExpenseRepository
    {
        Task<DailyExpenseData> GetDailyExpenseDataAsync(DailyExpenseRequestDto request);
    }
}