// Dtos/RequestDtos/Loan/OtherReports/LoanFollowUpRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanFollowUpRequestDto
    {
        // Search-by-member mode: set MemberId (the human-readable code, e.g. "M-001")
        // to look up the member and filter by them specifically.
        public string? MemberId { get; set; }

        // Search-by-date-range mode: used only when MemberId is not supplied.
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // "Member Id" | "Member Name" | "Account No" | "Follow Up Person" | "Follow Up Date" | "Follow Up By"
        public string OrderBy { get; set; } = "-1";
    }

    // Columns match sp_7_16_LoanFollowUpReport's output SELECT list
    public class LoanFollowUpRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? FollowUpPerson { get; set; }
        public DateTime? FollowUpDateOn { get; set; }
        public string? FollowUpBy { get; set; }
        public string? Remarks { get; set; }
        public decimal? LoanBalance { get; set; }
    }

    public class LoanFollowUpData
    {
        public List<LoanFollowUpRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? OrderBy { get; set; }
    }
}