// Inteface/ServiceInterface/MemberAccount/OthersReport/ITellerCashBalanceRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ITellerCashBalanceRepository
    {
        Task<TellerCashBalanceData> GetReportDataAsync(TellerCashBalanceRequestDto request);
    }
}