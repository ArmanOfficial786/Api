// Dtos/RequestDtos/Loan/OtherReports/LoanAccountClosedRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanAccountClosedRequestDto
    {
        // Date range in BS format (from/to)
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // "MemberId" | "FullName" | "AccountNo" | "LoanTypeName" | "LoanClosedDate"
        public string OrderBy { get; set; } = "MemberId";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanAccountClosedReport's output SELECT list
    public class LoanAccountClosedRowDto
    {
        public long? LmtLoanIssueId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? LoanCloseDate { get; set; }
        public decimal? LoanCloseAmount { get; set; }
        public string? MobileNo { get; set; }
        public string? TemporaryAddressDetail { get; set; }
        public string? CollectionCenterName { get; set; }
    }

    public class LoanAccountClosedData
    {
        public List<LoanAccountClosedRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalLoanCloseAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}