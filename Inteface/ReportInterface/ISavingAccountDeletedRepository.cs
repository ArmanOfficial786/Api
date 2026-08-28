// Inteface/ServiceInterface/MemberAccount/OthersReport/ISavingAccountDeletedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingAccountDeletedRepository
    {
        Task<SavingAccountDeletedData> GetReportDataAsync(SavingAccountDeletedRequestDto request);
    }
}