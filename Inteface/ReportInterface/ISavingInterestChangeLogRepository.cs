// Inteface/ServiceInterface/MemberAccount/OthersReport/ISavingInterestChangeLogRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingInterestChangeLogRepository
    {
        Task<SavingInterestChangeLogData> GetReportDataAsync(SavingInterestChangeLogRequestDto request);
    }
}