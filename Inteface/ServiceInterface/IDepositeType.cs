using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface IDepositeType
    {
        Task<List<DepositTypeResponse>> GetAllDepositeType();
    }
}
