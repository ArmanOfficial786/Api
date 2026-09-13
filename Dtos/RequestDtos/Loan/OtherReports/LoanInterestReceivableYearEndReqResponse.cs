// Dtos/RequestDtos/Loan/OtherReports/LoanInterestReceivableYearEndRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInterestReceivableYearEndRequestDto
    {
        // Selection type: "As" (As on date), "Monthly", "Yearly"
        public string SelectType { get; set; } = "As";

        // Date (As on date) in BS format - used when SelectType = "As"
        public string? AsOnDateBs { get; set; }

        // Monthly mode: Year and Month (BS)
        public int? YearlyYear { get; set; }
        public int? MonthlyYear { get; set; }
        public int? MonthlyMonth { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // "MemberId" | "FullName" | "LoanAccountNo" | "InterestAmount" | "LoanTypeName"
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanInterestReceivableYearEndReport's output SELECT list
    public class LoanInterestReceivableYearEndRowDto
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

    public class LoanInterestReceivableYearEndData
    {
        public List<LoanInterestReceivableYearEndRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalDepositAmount { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public string? AsOnDate { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? SelectType { get; set; }
        public string? OrderBy { get; set; }
    }
}