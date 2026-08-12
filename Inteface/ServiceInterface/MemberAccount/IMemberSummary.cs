using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IMemberSummary
    {
        Task<MemberSummaryData> GetMemberSummaryReport(MemberSummaryRequest request);
    }
}
