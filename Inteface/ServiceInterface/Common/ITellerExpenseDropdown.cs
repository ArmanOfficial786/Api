using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ITellerExpenseDropdown
    {
        Task<List<TellerLookupResponse>> GetTellersAsync(DateTime fromDate, DateTime toDate, long userId);
    }
}
