// Interfaces/ServiceInterface/MemberAccount/ChequeBookReport/IChequeBookIssueRepository.cs
using NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport;

namespace NexgenCosysReport.Inteface.ServiceInterface.MemberAccount.ChequeBookReport
{
    public interface IChequeBookIssueRepository
    {
        Task<ChequeBookIssueData> GetReportDataAsync(ChequeBookIssueRequestDto request);
        Task<MemberInfoDto?> GetMemberByMemberIdAsync(string memberId);
    }

    public class MemberInfoDto
    {
        public long MemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
    }
}