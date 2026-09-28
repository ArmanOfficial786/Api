using NexgenCosysReport.Dtos.RequestDtos.Common;

namespace NexgenCosysReport.Inteface.ServiceInterface.Common
{
    public interface IRemittanceType
    {
        Task<List<RemittanceTypeResponse>> GetAllActive();
    }
}
