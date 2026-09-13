// Dtos/RequestDtos/Account/IBTReports/IBTTransactionRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Account.IBTReports
{
    public class IBTTransactionRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }           // single office id, "-1" = All (dropdown, not checkbox list)

        // "FullName" (Member Name) or "MemMemberRegistrationOfficeName" (Branch Name) — matches ddlOrdertBy values
        public string OrderBy { get; set; } = "-1";
    }

    // Columns match sp_5_43_GetSavingIBTTransaction's output SELECT list
    public class IBTTransactionRowDto
    {
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? AccountNo { get; set; }
        public string? MemMemberRegistrationOfficeName { get; set; }
        public string? TransactionOnBs { get; set; }
        public decimal? Amount { get; set; }
        public string? PayableBranchName { get; set; }
    }

    public class IBTTransactionData
    {
        public List<IBTTransactionRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
    }
}