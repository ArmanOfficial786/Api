using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IFamAssetNameLookup
    {
        Task<FamAssetNameLookupDto?> GetByIdAsync(long famFixedAssetsDetailId);
    }
}
