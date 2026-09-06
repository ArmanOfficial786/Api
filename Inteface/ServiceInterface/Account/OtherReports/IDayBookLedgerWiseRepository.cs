// Inteface/ServiceInterface/Account/OtherReports/IDayBookLedgerWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.OtherReports
{
    public interface IDayBookLedgerWiseRepository
    {
        Task<DayBookLedgerWiseData> GetDayBookLedgerWiseDataAsync(DayBookLedgerWiseRequestDto request);
    }
}