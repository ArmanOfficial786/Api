using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface IMemberAccDeactive
    {
        Task<MemberAccountDeactiveData> GetMemberAccountDeactive(MemberAccountDeactiveRequest request);
    }
}
