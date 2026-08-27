using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ITellerExpense
    {
        Task<TellerWiseExpenseData> GetReportDataAsync(TellerWiseExpenseRequestDto request);
    }
}
