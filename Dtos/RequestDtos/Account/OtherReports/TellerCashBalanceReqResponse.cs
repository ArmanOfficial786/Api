// Dtos/RequestDtos/MemberAccount/OthersReport/TellerCashBalanceRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{
    public class TellerCashBalanceRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }          // "-1" = All; SP only supports a single office id
        public string OrderBy { get; set; } = "-1";     // "TellerName" or "Date"
        public bool NepaliReport { get; set; }
        public bool VisualReport { get; set; }
    }

    public class TellerCashBalanceRowDto
    {
        public string? Date { get; set; }
        public DateTime? DateOn { get; set; }
        public string? TellerName { get; set; }
        public long? UsmUserId { get; set; }
        public long? UsmOfficeId { get; set; }
        public decimal? TellerCashFromVault { get; set; }
        public decimal? TellerFrom { get; set; }
        public decimal? CashCollection { get; set; }
        public decimal? CashExpense { get; set; }
        public decimal? CashBalance { get; set; }
        public decimal? TellerTo { get; set; }
        public decimal? TellerCashToVault { get; set; }
        public decimal? ClosingBalance { get; set; }
    }

    public class TellerCashBalanceData
    {
        public List<TellerCashBalanceRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalClosingBalance { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? OfficeName { get; set; }
        public string? OrderBy { get; set; }
        public bool NepaliReport { get; set; }
    }
}