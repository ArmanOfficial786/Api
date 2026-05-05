using JsSampleReport.Dtos.RequestDtos.Common;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IDepositeType
    {
        Task<List<DepositTypeResponse>> GetAllDepositeType();
    }
}
