// Inteface/ServiceInterface/MemberAccount/OthersReport/ISavingIssueRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.OthersReport
{
    public interface ISavingIssueRepository
    {
        Task<SavingIssueData> GetReportDataAsync(SavingIssueRequestDto request);
    }
}