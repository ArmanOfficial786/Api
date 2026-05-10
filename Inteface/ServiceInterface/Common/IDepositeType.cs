using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IDepositeType
    {
        Task<List<DepositTypeResponse>> GetAllDepositeType();
    }
}
