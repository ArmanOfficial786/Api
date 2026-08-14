namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class AccountLookUpDtos
    {
        public long MamAccountOpeningId { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string AccountNo { get; set; } = string.Empty;
        public string? DepositType { get; set; }
        public string? AccountType { get; set; }
        public decimal? InterestRate { get; set; }
        public string? OpenedDate { get; set; }
        public string? MaturityDate { get; set; }
        public string? Status { get; set; }
        public long UsmOfficeId { get; set; }
        public string? OfficeName { get; set; }
    }

    public class AccountSelectedDto
    {
        public long MamAccountOpeningId { get; set; }
        public string AccountNo { get; set; } = string.Empty;
        public long MemMemberRegistrationId { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public long UsmOfficeId { get; set; }
        public bool AccountNamingOption { get; set; }
        public string? AccountName { get; set; }
    }
}
