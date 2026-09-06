// Inteface/ServiceInterface/AccountOperation/OthersReport/IDayBookVoucherWiseRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports;
//using NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.AccountOperation.OthersReport
{
    public interface IDayBookVoucherWiseRepository
    {
        Task<DayBookVoucherWiseData> GetReportDataAsync(DayBookVoucherWiseRequestDto request);
    }
}