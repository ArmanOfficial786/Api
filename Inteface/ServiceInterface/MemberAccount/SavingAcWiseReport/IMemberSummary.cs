using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IMemberSummary
    {
        Task<MemberSummaryData> GetMemberSummaryReport(MemberSummaryRequest request);
    }
}
