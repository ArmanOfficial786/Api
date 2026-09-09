// Dtos/RequestDtos/AccountOperation/OthersReport/BankReceivedPaymentRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.OthersReport
{
    public class BankReceivedPaymentRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public bool SameCompanyName { get; set; } = true;
        public string PaymentType { get; set; } = "Bank Payment";
        public string TransactionType { get; set; } = "all";
        public string OrderBy { get; set; } = "Transaction Date";
    }

    public class BankReceivedPaymentRowDto
    {
        public long? AcoTransactionId { get; set; }
        public string? Accountno { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? TransactionOn { get; set; }
        public string? TransactionOnBs { get; set; }
        public string? TransactionType { get; set; }
        public string? Description { get; set; }
        public decimal? CashReceived { get; set; }
        public string? LedgerHead { get; set; }
    }

    public class BankReceivedPaymentData
    {
        public List<BankReceivedPaymentRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? PaymentType { get; set; }
        public string? TransactionType { get; set; }
        public string? OrderBy { get; set; }
    }
}