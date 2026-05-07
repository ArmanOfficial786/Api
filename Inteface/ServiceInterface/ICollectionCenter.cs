using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface
{
    public interface ICollectionCenter
    {
        Task<List<CollectionCenterResponseDto>> GetCollectionCenters(long lstOfficeId);
    }
}
