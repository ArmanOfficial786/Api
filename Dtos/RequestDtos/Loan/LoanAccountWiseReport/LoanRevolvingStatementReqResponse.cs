
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanRevolvingStatementRequestDto
    {

        public string AccountNo { get; set; } = string.Empty;


        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        public bool SameCompanyName { get; set; } = true;


        public bool VisualReport { get; set; } = false;
    }


    public class LoanRevolvingStatementRowDto
    {
        public string? LoanAccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? TransactionOnBs { get; set; }
        public DateTime? TransactionOn { get; set; }
        public string? VoucherNo { get; set; }
        public string? Particulars { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? Balance { get; set; }
        public string? Narration { get; set; }
        public string? EnteredBy { get; set; }
        public string? ChequeNo { get; set; }
    }

    public class LoanRevolvingStatementData
    {
        public List<LoanRevolvingStatementRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? VerifiedTill { get; set; }
    }
}