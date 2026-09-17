namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInterestReceivableYearEndRequestDto
    {
        public string SelectType { get; set; } = "As";
        public string? AsOnDateBs { get; set; }
        public int? YearlyYear { get; set; }
        public int? MonthlyYear { get; set; }
        public int? MonthlyMonth { get; set; }
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

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