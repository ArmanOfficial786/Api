using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.SavingAcWiseReport
{
    public interface ISMSCategory
    {
        Task<SMSCategoryData> GetSMSCategory(SMSCategoryRequest request);
    }
}
