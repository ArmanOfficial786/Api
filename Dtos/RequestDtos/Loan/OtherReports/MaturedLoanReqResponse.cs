// Dtos/RequestDtos/Loan/OtherReports/MaturedLoanRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class MaturedLoanRequestDto
    {
        // Search-by-member mode: set MemberId to filter by specific member
        public string? MemberId { get; set; }

        // Search-by-date-range mode: used when MemberId is not supplied.
        // Filters by ls.MaturityOn between FromDateBs and ToDateBs
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // "MemberId" | "FullName" | "MaturityDate" | "LoanAccountNo" | "DisburseAmount" | "DueBalance"
        public string OrderBy { get; set; } = "MemberId";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_MaturedLoanReport's output SELECT list
    public class MaturedLoanRowDto
    {
        public long? LmtLoanIssueId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? MaturityDate { get; set; }
        public decimal? TotalDueAmount { get; set; }
        public decimal? TotalPaidAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? MobileNo { get; set; }
        public string? TemporaryAddressDetail { get; set; }
        public string? CollectionCenterName { get; set; }
    }

    public class MaturedLoanData
    {
        public List<MaturedLoanRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalDueAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}