namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanPaymentRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? PaymentBy { get; set; }
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "-1";
        public bool VisualReport { get; set; } = false;
    }


    public class LoanPaymentRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? IssueDateBS { get; set; }
        public DateTime? IssueDateAD { get; set; }
        public long? LmtLoanIssueId { get; set; }
        public string? LoanAccountNoFirst { get; set; }
        public string? LoanAccountNoLast { get; set; }
        public string? InteRate { get; set; }
        public string? Period { get; set; }
        public long? AccountTypeId { get; set; }
        public string? PaymentBy { get; set; }
        public string? AccNoBank { get; set; }
        public string? ChequeNo { get; set; }
        public decimal? Amount { get; set; }
    }

    public class LoanPaymentData
    {
        public List<LoanPaymentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PaymentBy { get; set; }
        public string? OrderBy { get; set; }
    }
}