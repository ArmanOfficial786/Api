using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface IDepositeType
    {
        Task<List<DepositTypeResponse>> GetAllDepositeType();
    }
}
