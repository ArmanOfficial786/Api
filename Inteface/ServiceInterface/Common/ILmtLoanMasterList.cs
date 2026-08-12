using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface ILmtLoanMasterList
    {
        Task<List<LmtLoanMaseterListResponse>> GetAllLmtLoanMasterList();
    }
}
