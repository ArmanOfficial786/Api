using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IAssetCategory
    {
        Task<List<AssetCategoryResponse>> GetAllByType(char type);
    }
}
