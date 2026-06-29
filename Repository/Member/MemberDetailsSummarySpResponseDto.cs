using NexgenCosysReport.Dtos.RequestDtos.Member;

namespace NexgenCosysReport.Repository.Member
{
    internal class MemberDetailsSummarySpResponseDto
    {
        public object MemberInfo { get; set; }
        public List<ShareAccountDto> ShareAccounts { get; set; }
        public List<SavingAccountDto> SavingAccounts { get; set; }
        public List<LoanIssueDto> LoanIssues { get; set; }
        public List<GroupGuaranteeDto> GroupGuarantees { get; set; }
        public int TotalShareRecords { get; set; }
        public int TotalSavingRecords { get; set; }
        public int TotalLoanRecords { get; set; }
        public int TotalGuaranteeRecords { get; set; }
    }
}