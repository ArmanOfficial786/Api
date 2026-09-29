
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    //public class LoanRevolvingStatementRequestDto
    //{

    //    public string AccountNo { get; set; } = string.Empty;
    //    public string? FromDateBs { get; set; }
    //    public string? ToDateBs { get; set; }
    //    public bool SameCompanyName { get; set; } = true;
    //    public bool VisualReport { get; set; } = false;
    //}


    //public class LoanRevolvingStatementRowDto
    //{
    //    public string? LoanAccountNo { get; set; }
    //    public string? MemberId { get; set; }
    //    public string? MemberName { get; set; }
    //    public string? TransactionOnBs { get; set; }
    //    public DateTime? TransactionOn { get; set; }
    //    public string? VoucherNo { get; set; }
    //    public string? Particulars { get; set; }
    //    public decimal? DebitAmount { get; set; }
    //    public decimal? CreditAmount { get; set; }
    //    public decimal? Balance { get; set; }
    //    public string? Narration { get; set; }
    //    public string? EnteredBy { get; set; }
    //    public string? ChequeNo { get; set; }
    //}

    //public class LoanRevolvingStatementData
    //{
    //    public List<LoanRevolvingStatementRowDto> Rows { get; set; } = [];
    //    public int TotalRecords { get; set; }
    //    public decimal TotalDebitAmount { get; set; }
    //    public decimal TotalCreditAmount { get; set; }
    //    public decimal OpeningBalance { get; set; }
    //    public decimal ClosingBalance { get; set; }
    //    public string? FromDateBs { get; set; }
    //    public string? ToDateBs { get; set; }
    //    public string? AccountNo { get; set; }
    //    public string? MemberName { get; set; }
    //    public string? MemberId { get; set; }
    //    public string? VerifiedTill { get; set; }
    //}



    public class LoanRevolvingStatementRequestDto
    {
        public string AccountNo { get; set; } = string.Empty;
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    // Matches sp_7_16_LoanStatementOverDraftReport's #TEMP columns exactly:
    // LoanAccountNo, DateOn, DateOnBs, Description, LoanIssueAmount,
    // Principal, Fine, Interest, BalanceAmount. The previous version of this
    // DTO (Particulars/VoucherNo/ChequeNo/DebitAmount/CreditAmount/EnteredBy)
    // named columns the SP never returns, so Dapper bound nothing to them.
    public class LoanRevolvingStatementRowDto
    {
        public string? LoanAccountNo { get; set; }
        public DateTime? DateOn { get; set; }
        public string? DateOnBs { get; set; }
        public string? Description { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? Principal { get; set; }
        public decimal? Fine { get; set; }
        public decimal? Interest { get; set; }
        public decimal? BalanceAmount { get; set; }
    }

    public class LoanRevolvingStatementData
    {
        public List<LoanRevolvingStatementRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }

        public decimal TotalIssueAmount { get; set; }
        public decimal TotalPrincipal { get; set; }
        public decimal TotalInterest { get; set; }
        public decimal TotalFine { get; set; }

        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? AccountNo { get; set; }

        // Header info-box fields — matches the reference image
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? SanctionAmount { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanPeriod { get; set; }
        public string? LoanIssueOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
    }
}