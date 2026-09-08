// Dtos/RequestDtos/Account/OthersReport/PaymentThroughSavingRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport
{
    public class PaymentThroughSavingRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public bool SameCompanyName { get; set; } = true;

        // "all" | "saving" | "share" | "miscellaneous" — matches ddlTransactionType values (note: "loan" is commented out in this UI)
        public string TransactionType { get; set; } = "all";

        // "Transaction Date" or "Member Id" — matches ddlOrderBy SelectedItem text (legacy passes display text, not value)
        public string OrderBy { get; set; } = "Transaction Date";
    }

    // Columns match the SP's final SELECT list exactly
    public class PaymentThroughSavingRowDto
    {
        public long? AcoTransactionId { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? TransactionOn { get; set; }
        public string? TransactionOnBs { get; set; }
        public string? TransactionType { get; set; }
        public string? Description { get; set; }
        public decimal? CashReceived { get; set; }
    }

    public class PaymentThroughSavingData
    {
        public List<PaymentThroughSavingRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? TransactionType { get; set; }
        public string? OrderBy { get; set; }
    }
}