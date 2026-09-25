// Inteface/ServiceInterface/Microfinance/MicrofinanceReport/ISavingTypeWiseBalanceRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Microfinance.MicrofinanceReport
{
    public interface ISavingTypeWiseBalanceRepository
    {
        Task<SavingTypeWiseBalanceData> GetReportDataAsync(SavingTypeWiseBalanceRequestDto request);
    }
}