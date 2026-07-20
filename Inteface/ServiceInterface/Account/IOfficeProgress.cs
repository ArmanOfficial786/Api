using NexgenCosysReport.Dtos.RequestDtos.Account;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account
{
    public interface IOfficeProgress
    {
        Task<OfficeProgressData> GetOfficeProgress(OfficeProgressRequest request);
    }
}
