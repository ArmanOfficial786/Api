using NexgenCosysReport.Dtos.RequestDtos.MemberAccount;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount
{
    public interface ISMSCategory
    {
        Task<SMSCategoryData> GetSMSCategory(SMSCategoryRequest request);
    }
}
