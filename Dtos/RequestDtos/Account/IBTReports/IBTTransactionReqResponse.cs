namespace NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports
{
    public class IBTTransactionRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string OrderBy { get; set; } = "-1";
    }

    public class IBTTransactionRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public decimal? CashReceived { get; set; }
        public decimal? CashWithdrawl { get; set; }
        public string? TransactionOnBs { get; set; }
        public string? Description { get; set; }
        public string? TransactionOfficeName { get; set; }
        public string? MemMemberRegistrationOfficeName { get; set; }
    }

    public class IBTTransactionData
    {
        public List<IBTTransactionRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalCashReceived { get; set; }
        public decimal TotalCashWithdrawl { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
    }
}