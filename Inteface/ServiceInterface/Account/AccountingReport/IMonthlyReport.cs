using NexgenCosysReport.Dtos.ReportDtos;
using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IMonthlyReport
    {
        Task<MonthlyReportData> GetMonthlyReport(MonthlyReportRequest request);
    }
}
