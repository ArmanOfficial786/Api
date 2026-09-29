
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanStatementDateWiseRequestDto
    {

        public string AccountNo { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class LoanStatementDateWiseMemberInfoDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? MaturityDateBs { get; set; }
        public string? Period { get; set; }
        public string? InterestRate { get; set; }
        public string? PaymentType { get; set; }
        public string? Remark { get; set; }
    }


    public class LoanStatementDateWiseRowDto
    {
        public int? Sno { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanAccountNo { get; set; }
        public decimal? Principal { get; set; }
        public decimal? Interest { get; set; }
        public decimal? Fine { get; set; }
        public decimal? InstallmentAmt { get; set; }
        public decimal? BalanceAmount { get; set; }
        public string? DateOnBs { get; set; }
        public string? Narration { get; set; }
    }

    public class LoanStatementDateWiseData
    {
        public LoanStatementDateWiseMemberInfoDto? MemberInfo { get; set; }
        public List<LoanStatementDateWiseRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalFine { get; set; }
        public decimal TotalInstallmentAmt { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
    }
}