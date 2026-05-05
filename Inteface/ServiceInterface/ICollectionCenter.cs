using JsSampleReport.Dtos.RequestDtos.Common;

namespace JsSampleReport.Inteface.ServiceInterface
{
    public interface ICollectionCenter
    {
        Task<List<CollectionCenterResponseDto>> GetCollectionCenters(long lstOfficeId);
    }
}
