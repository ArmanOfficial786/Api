namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount
{
    public class MemberAccountDetailRequest
    {
        public string TillDate { get; set; } = string.Empty;           // Nepali "yyyy/MM/dd"
        public string BranchIds { get; set; } = "-1";                  // comma-separated office ids
        public string BranchName { get; set; } = "All";
        public string DepositTypeId { get; set; } = "-1";              // SycDepositTypeId
        public string MemberId { get; set; } = string.Empty;           // Member ID
        public string MemberName { get; set; } = string.Empty;         // Member Name
        public long? MemberRegistrationId { get; set; }           // MemMemberRegistrationId
        public int Status { get; set; }                            // 1=Opened, 2=Closed, 4=Suspended, 5=Disable, 3=With Balance
        public string CollectorId { get; set; } = "-1";                // HurCollectorId
        public string CollectionCenterId { get; set; } = "-1";         // SycCollectionCenterId
        public string MemberGroupId { get; set; } = "-1";              // SycMemberGroupId
        public bool EnableCollectionCenterGroup { get; set; } = false;
        public bool EnableMemberGroupGroup { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public string OrderBy { get; set; } = "Member Name";           // Member Name, Member Id, Account No, Interest Rate, Deposit, Withdrawl, Balance
        public List<string> SelectedColumns { get; set; } = new List<string>();
        public bool VisualReport { get; set; } = false;
    }

    public class MemberAccountDetailRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? ContactNo { get; set; }
        public string? BirthOnBS { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? SpouseName { get; set; }
        public string? RegisteredOn { get; set; }
        public string? SavingAccountType { get; set; }
        public string? AccountNo { get; set; }
        public string? AccountOpenOn { get; set; }
        public string? MatureDate { get; set; }
        public string? InterestTransferType { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? TaxRate { get; set; }
        public string? InterestTransferAccount { get; set; }
        public decimal? FreezeAmount { get; set; }
        public decimal? GuaranteeAmount { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public string? InstallmentType { get; set; }
        public int? DueCount { get; set; }
        public decimal? Deposit { get; set; }
        public decimal? Withdraw { get; set; }
        public decimal? Balance { get; set; }
        public string? CollectionCenter { get; set; }
        public string? MemberGroup { get; set; }
        public string? CollectorName { get; set; }
        public int? StatusId { get; set; }
        public string? StatusName { get; set; }
    }

    public class MemberAccountDetailData
    {
        public List<MemberAccountDetailRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class MemberAccountDetailReqResponse
    {
    }
}
