using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IFamClearanceStatusLookup
    {
        Task<List<FamClearanceStatusLookupDto>> GetAllAsync();
    }
}
