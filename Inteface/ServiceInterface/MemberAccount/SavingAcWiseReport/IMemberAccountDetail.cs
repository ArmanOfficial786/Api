using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IMemberAccountDetail
    {
        Task<MemberAccountDetailData> GetMemberAccountDetailReport(
            MemberAccountDetailRequest request,
            CancellationToken ct = default);
    }
}
