using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IShareType
    {
        Task<List<ShareTypeResponse>> GetAllLmtLoanMasterList();
    }
}
