// Inteface/ServiceInterface/MemberAccount/OthersReport/ISavingAccountClosedRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingAccountClosedRepository
    {
        Task<SavingAccountClosedData> GetReportDataAsync(SavingAccountClosedRequestDto request);
    }
}