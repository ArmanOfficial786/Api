// Dtos/RequestDtos/Loan/OtherReports/LoanInterestDiscountRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanInterestDiscountRequestDto
    {
        // Member ID (human-readable code, e.g. "M-001") - optional
        public string? MemberId { get; set; }

        // Loan Type filter (-1 = all loan types)
        public long LoanTypeId { get; set; } = -1;

        // Date range in BS format
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }

        // Comma-separated office ids from the checkbox list ("-1" or empty = all offices)
        public string? BranchIds { get; set; }

        // "Date" | "MemberId" | "FullName" | "LoanType" | "LoanAcAmount" | "Amount" | "-1"
        public string OrderBy { get; set; } = "-1";

        // Visual report flag
        public bool VisualReport { get; set; } = false;
    }

    // Columns match sp_7_16_LoanInterestDiscountReport's output SELECT list
    public class LoanInterestDiscountRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? CashReceived { get; set; }
        public string? Operator { get; set; }
        public string? TransactionOnBs { get; set; }
    }

    public class LoanInterestDiscountData
    {
        public List<LoanInterestDiscountRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberName { get; set; }
        public string? LoanTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}