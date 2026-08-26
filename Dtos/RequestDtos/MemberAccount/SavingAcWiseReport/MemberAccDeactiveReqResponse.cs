namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport
{

    public class MemberAccountDeactiveRequest
    {
        public string TillDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string? BranchIds { get; set; }                 // comma-separated
        public string BranchName { get; set; } = "All";
        public int DuePeriod { get; set; } = 365;              // days
        public string TransactionType { get; set; } = "S";     // S=Saving, L=Loan
        public long TypeId { get; set; }
        public bool IsActive { get; set; } = true;             // true=Active, false=Inactive
        public string OrderBy { get; set; } = "Member Name";
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class MemberAccountDeactiveRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? Type { get; set; }              // Saving Type or Loan Type
        public string? LastTransactionDate { get; set; }
        public int Age { get; set; }                    // Days since last transaction
        public decimal Balance { get; set; }
        public string? Status { get; set; }             // Active / Inactive
        public string? Address { get; set; }             // new — matches image's "Address" column
        public string? ContactNo { get; set; }           // new — matches image's "Contact No" column
    }

    public class MemberAccountDeactiveData
    {
        public List<MemberAccountDeactiveRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }


    public class MemberAccDeactiveReqResponse
    {
    }
}
