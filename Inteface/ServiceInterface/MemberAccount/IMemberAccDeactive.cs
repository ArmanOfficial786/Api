using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface IMemberAccDeactive
    {
        Task<MemberAccountDeactiveData> GetMemberAccountDeactive(MemberAccountDeactiveRequest request);
    }
}
