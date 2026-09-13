// Dtos/RequestDtos/Loan/OtherReports/LoanInterestReceivableMonthlyRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInterestReceivableMonthlyRequestDto
    {
        // Till date in BS format (required)
        public string TillDateBs { get; set; } = string.Empty;

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // "MemberId" | "FullName" | "LoanAccountNo" | "InterestAmount"
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanInterestReceivableMonthlyReport's output SELECT list
    public class LoanInterestReceivableMonthlyRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public long? LmtLoanPaymentTypeId { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? DepositAmount { get; set; }
        public decimal? Balance { get; set; }
        public decimal? InterestAmount { get; set; }
        public DateTime? LastPaymentOnAD { get; set; }
    }

    public class LoanInterestReceivableMonthlyData
    {
        public List<LoanInterestReceivableMonthlyRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalDepositAmount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}