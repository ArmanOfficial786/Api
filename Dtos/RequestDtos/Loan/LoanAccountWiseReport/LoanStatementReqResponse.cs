
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanStatementRequestDto
    {

        public string AccountNo { get; set; } = string.Empty;

        public string? BranchIds { get; set; }

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        public string ReportType { get; set; } = "English";

        public bool ShowEntryBy { get; set; } = false;

        public bool ShowDetailNarration { get; set; } = false;

        public bool ShowAccountCloseDetails { get; set; } = false;

        public bool EnableBillNumber { get; set; } = false;

        public string NarrationType { get; set; } = "Default";


        public bool SameCompanyName { get; set; } = true;

        public bool VisualReport { get; set; } = false;
    }


    public class LoanStatementMemberInfoDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public string? ContactAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanType { get; set; }
        public string? Period { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountStatus { get; set; }
    }

    public class LoanStatementTransactionDto
    {
        public string? TransactionOnBs { get; set; }
        public DateTime? TransactionOn { get; set; }
        public string? VoucherNo { get; set; }
        public string? Particulars { get; set; }
        public string? Narration { get; set; }
        public string? EnteredBy { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? Balance { get; set; }
        public string? ChequeNo { get; set; }
    }

    public class LoanStatementGuaranteeDto
    {
        public string? GuarantorName { get; set; }
        public string? GuarantorMemberId { get; set; }
        public string? GuaranteeAmount { get; set; }
        public string? GuaranteeShareAmount { get; set; }
        public string? GuaranteeDateOnBs { get; set; }
        public string? AccountNo { get; set; }
    }

    public class LoanStatementClosingDueDto
    {
        public string? Title { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
    }

    public class LoanStatementData
    {
        public LoanStatementMemberInfoDto? MemberInfo { get; set; }
        public List<LoanStatementTransactionDto> Transactions { get; set; } = [];
        public List<LoanStatementGuaranteeDto> Guarantees { get; set; } = [];
        public List<LoanStatementClosingDueDto> ClosingDues { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? ReportType { get; set; }
        public string? VerifiedTill { get; set; }
    }
}