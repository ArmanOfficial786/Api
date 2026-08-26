namespace NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport
{
    public class ThresholdTransactionRequest
    {
        public string FromDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }                 // comma-separated
        public string BranchName { get; set; } = "All";
        public string? TransactionNumber { get; set; }         // Bill No
        public string? MemberName { get; set; }
        public string? OrderBy { get; set; }    // Member Id, Bill No, Date, Account No
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class ThresholdTransactionRowDto
    {
        public long AcoThresholdTransactionId { get; set; }
        public long AcoTransactionId { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? NameOfDepositor { get; set; }
        public string? AddressOfDepositor { get; set; }
        public string? ContactNo { get; set; }
        public string? Branch { get; set; }
        public long? UsmOfficeId { get; set; }
        public DateTime? DateOfTransactionOn { get; set; }
        public string? DateOfTransactionOnBS { get; set; }
        public string? NatureOfTransaction { get; set; }
        public string? DepositTypeName { get; set; }
        public string? AccountNo { get; set; }
        public decimal AmountInvolvedInDeposit { get; set; }
        public decimal AmountInvolvedInWithdraw { get; set; }
        public string? TransactionNumber { get; set; }
        public string? SourceOfFund { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
    }

    public class ThresholdTransactionData
    {
        public List<ThresholdTransactionRowDto> Rows { get; set; } = new();
        public decimal TotalDepositAmount { get; set; }
        public decimal TotalWithdrawAmount { get; set; }
        public int TotalRecords { get; set; }
    }
}
