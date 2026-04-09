using JsSampleReport.Dtos.RequestDtos;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface ICollectionCenter
    {
        Task<List<CollectionCenterResponseDto>> GetCollectionCenters(long lstOfficeId);
    }
}
