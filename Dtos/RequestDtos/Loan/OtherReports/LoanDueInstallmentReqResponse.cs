namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanDueInstallmentRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? MemberId { get; set; }
        public int LmtPaymentDurationTypeId { get; set; } = -1;
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }

    public class LoanDueInstallmentRowDto
    {
        public long? LmtLoanIssueId { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanTypeName { get; set; }
        public string? PaymentDurationType { get; set; }
        public decimal? PrincipleAmount { get; set; }
        public decimal? InterestAmount { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public decimal? BalanceAmount { get; set; }
        public DateTime? DateOnAD { get; set; }
        public string? DateOnBS { get; set; }
        public string? TemporaryAddressDetail { get; set; }
        public string? MobileNo { get; set; }
    }

    public class LoanDueInstallmentData
    {
        public List<LoanDueInstallmentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalPrincipleAmount { get; set; }
        public decimal TotalInterestAmount { get; set; }
        public decimal TotalInstallmentAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? MemberName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PaymentDurationTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}