namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInterestReceivableMonthlyRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

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