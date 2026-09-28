namespace NexgenCosysReport.Dtos.RequestDtos.Asset
{
    public class FamPurchaseReportRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long CategoryId { get; set; } = -1;
        public string ReportType { get; set; } = "ASSET";
        public string OrderBy { get; set; } = "";
        public bool VisualReport { get; set; } = false;
    }

    public class FamPurchaseReportRowDto
    {
        public string? Code { get; set; }
        public string? FixedAssetsName { get; set; }
        public string? SerialNumber { get; set; }
        public string? ModelName { get; set; }
        public string? IdentificationDetail { get; set; }
        public decimal? Amount { get; set; }
        public string? PurchaseType { get; set; }
        public string? PurchaseDateOnBs { get; set; }
        public string? TransactionOnBs { get; set; }
        public string? LastDepreciationDateOnBs { get; set; }
        public decimal? LastDepreciatedValue { get; set; }
        public string? Remarks { get; set; }
    }

    public class FamPurchaseReportData
    {
        public List<FamPurchaseReportRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalLastDepreciatedValue { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? CategoryName { get; set; }
        public string? ReportType { get; set; }
        public string? OrderBy { get; set; }
    }
}

