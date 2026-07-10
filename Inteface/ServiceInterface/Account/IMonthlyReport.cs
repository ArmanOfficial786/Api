using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IMonthlyReport
    {
        Task<MonthlyReportData> GetMonthlyReport(MonthlyReportRequest request);
    }
}
