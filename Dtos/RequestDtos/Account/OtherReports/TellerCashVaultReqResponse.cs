namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class TellerCashVaultRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public bool SameCompanyName { get; set; } = true;
        public string OrderBy { get; set; } = "-1";
        public bool Type { get; set; }
    }

    // Columns match sp_6_56_GetTellerCashFromVault / sp_6_56_GetTellerCashToVault SELECT aliases exactly
    public class TellerCashVaultRowDto
    {
        public string? TellerName { get; set; }
        public string? FromVaultBy { get; set; }
        public string? OfficeName { get; set; }
        public string? Date { get; set; }
        public decimal? Amount { get; set; }
        public int? Rs1 { get; set; }
        public int? Rs2 { get; set; }
        public int? Rs5 { get; set; }
        public int? Rs10 { get; set; }
        public int? Rs20 { get; set; }
        public int? Rs25 { get; set; }
        public int? Rs50 { get; set; }
        public int? Rs100 { get; set; }
        public int? Rs250 { get; set; }
        public int? Rs500 { get; set; }
        public int? Rs1000 { get; set; }
        public int? Paisa { get; set; }
    }

    public class TellerCashVaultData
    {
        public List<TellerCashVaultRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportType { get; set; } // reflects what actually ran, after any fallback resolution
    }


    public class TellerCashVaultReqResponse
    {
    }
}
