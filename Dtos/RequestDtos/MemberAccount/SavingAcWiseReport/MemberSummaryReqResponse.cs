namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport
{
    public class MemberSummaryRequest
    {
        public string TillDate { get; set; } = string.Empty;           // Nepali "yyyy/MM/dd"
        public string BranchIds { get; set; } = "-1";                  // comma-separated office ids
        public string BranchName { get; set; } = "All";
        public string CollectionCenterId { get; set; } = "-1";         // SycCollectionCenterId
        public string MemberGroupId { get; set; } = "-1";              // SycMemberGroupId
        public bool EnableCollectionCenterGroup { get; set; } = false;
        public bool EnableMemberGroupGroup { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public string OrderBy { get; set; } = "Member Id";             // Member Id, Member Name, Address, Share Amount, etc.
        public bool VisualReport { get; set; } = false;
    }
    public class MemberSummaryRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CollectionCenter { get; set; }
        public string? MemberGroup { get; set; }
        public decimal ShareAmount { get; set; }
        public decimal NormalSaving { get; set; }
        public decimal RecurringSaving { get; set; }
        public decimal FixedSaving { get; set; }
        public decimal TermSaving { get; set; }
        public decimal DoubleDeposit { get; set; }
        public decimal RegularSaving { get; set; }
        public decimal TotalSaving { get; set; }
        public decimal LoanAmount { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class MemberSummaryData
    {
        public List<MemberSummaryRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalShareAmount { get; set; }
        public decimal TotalSaving { get; set; }
        public decimal TotalLoan { get; set; }
        public decimal GrandTotal { get; set; }
        public Dictionary<string, decimal> SavingTypeTotals { get; set; } = new();
    }

    public class MemberSummaryReqResponse
    {
    }
}
