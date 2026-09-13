// Dtos/RequestDtos/Loan/OtherReports/LoanPaymentRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanPaymentRequestDto
    {
        // Date range in BS format (from/to) - filters by tm.IssueDateAD
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        // Payment method filter: null/empty = All, "BANK", "SAVING", "CASH"
        public string? PaymentBy { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // Member Group filter (empty/-1 = all groups)
        public string? MemberGroupId { get; set; }

        // "MemberId" | "FullName" | "AccountNo" | "LoanTypeName" | "LoanIssueAmount" 
        // | "InterestRate" | "Period" | "LoanIssueDate" | "Amount"
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanPaymentReport's output SELECT list
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