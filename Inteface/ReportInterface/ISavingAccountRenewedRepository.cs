// Inteface/ServiceInterface/MemberAccount/OthersReport/ISavingAccountRenewedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingAccountRenewedRepository
    {
        Task<SavingAccountRenewedData> GetReportDataAsync(SavingAccountRenewedRequestDto request);
    }
}