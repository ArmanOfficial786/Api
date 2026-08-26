using NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.Account.AccountingReport
{
    public interface IOfficeProgress
    {
        Task<OfficeProgressData> GetOfficeProgress(OfficeProgressRequest request);
    }
}
