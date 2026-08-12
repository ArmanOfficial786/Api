// Dtos/RequestDtos/MemberAccount/MemberAccountDetailNoRequest.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount
{
    public class MemberAccountDetailNoRequest
    {
        public string TillDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string? BranchIds { get; set; }                 // comma-separated
        public string BranchName { get; set; } = "All";
        public int MemberType { get; set; } = 0;               // 0=All, 1=Active, 2=Inactive
        public bool IncludeSaving { get; set; } = true;
        public bool IncludeShare { get; set; } = true;
        public bool IncludeLoan { get; set; } = true;
        public string? SavingTypeId { get; set; }              // SycDepositTypeId
        public string? ShareTypeId { get; set; }               // ShmShareTypeId
        public string? LoanTypeId { get; set; }                // LmtLoanTypeMasterId
        public string OrderBy { get; set; } = "Member Name";
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    // One row PER MEMBER — matches the legacy report layout exactly:
    // SN | MemberId | Name | Address | Contact No | Saving | Share | Loan
    public class MemberAccountDetailNoRowDto
    {
        public string? MemberId { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? ContactNo { get; set; }
        public decimal Saving { get; set; }
        public decimal Share { get; set; }
        public decimal Loan { get; set; }
    }

    public class MemberAccountDetailNoData
    {
        public List<MemberAccountDetailNoRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalSaving { get; set; }
        public decimal TotalShare { get; set; }
        public decimal TotalLoan { get; set; }
    }

    public class MemberAccDetailListReqResponse
    {
    }
}