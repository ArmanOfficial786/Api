namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.ChequeBookReport
{
    public class ChequeBookWithdrawalRequestDto
    {
        public long AccountId { get; set; } = -1;
        public string AccountNo { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string OrderBy { get; set; } = "Cheque No";
        public bool VisualReport { get; set; } = false;
    }

    public class ChequeBookWithdrawalRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public long? ChequeNo { get; set; }
        public string? ChequeDate { get; set; }
        public string? ChequeDateBs { get; set; }
        public string? WithdrawalDate { get; set; }
        public string? WithdrawalDateBs { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string? BranchName { get; set; }
        public string? Operator { get; set; }
        public string? LastModifiedOn { get; set; }
        public string? LastModifiedBy { get; set; }
        public decimal? WithdrawalAmount { get; set; }
        public string? ChequeWithdrawStatus { get; set; }
    }

    public class ChequeBookWithdrawalData
    {
        public List<ChequeBookWithdrawalRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public int TotalWithdrawals { get; set; }
        public string? AccountNo { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? OrderBy { get; set; }
    }
    public class ChequeBookWithdrawalReqResponse
    {
    }
}
