// Inteface/ServiceInterface/Loan/OtherReports/IMiscellaneousIncomeRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports;

namespace NexgenCosysReport.Inteface.ServiceInterface.Loan.OtherReports
{
    public interface IMiscellaneousIncomeRepository
    {
        Task<MiscellaneousIncomeData> GetReportDataAsync(MiscellaneousIncomeRequestDto request);
    }
}