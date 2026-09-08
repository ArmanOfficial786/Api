namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class TellerToTellerCashTransferRequestDto
    {
        public string FromDate { get; set; } = string.Empty;
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string OrderBy { get; set; } = "Date";
        public bool VisualReport { get; set; } = false;
    }

    public class TellerToTellerCashTransferRowDto
    {
        public string? TellerFrom { get; set; }
        public string? TellerTo { get; set; }
        public string? OfficeName { get; set; }
        public string? Date { get; set; }
        public decimal? Amount { get; set; }
        public string? IssuedBy { get; set; }
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

    public class TellerToTellerCashTransferData
    {
        public List<TellerToTellerCashTransferRowDto> Rows { get; set; } = new List<TellerToTellerCashTransferRowDto>();
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
    }
    public class TellerToTellerCashTransferReqResponse
    {
    }
}
