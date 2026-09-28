namespace NexgenCosysReport.Dtos.RequestDtos.Asset
{
    public class FamDepreciationReportRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public string OrderBy { get; set; } = "";
        public bool VisualReport { get; set; } = false;
    }

    public class FamDepreciationReportRowDto
    {
        public long? FamDepreciationTransactionId { get; set; }
        public string? Code { get; set; }
        public string? FixedAssetsName { get; set; }
        public string? PurchaseDateOnBs { get; set; }
        public decimal? PurchaseAmt { get; set; }
        public string? MethodName { get; set; }
        public decimal? Percentage { get; set; }
        public string? DepreciationOnBs { get; set; }
        public decimal? PrevAssetValue { get; set; }
        public decimal? DepreciationAmt { get; set; }
        public decimal? NewAssetValue { get; set; }
        public string? DepreciationBranch { get; set; }
    }

    public class FamDepreciationReportData
    {
        public List<FamDepreciationReportRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalPurchaseAmt { get; set; }
        public decimal TotalDepreciationAmt { get; set; }
        public decimal TotalNewAssetValue { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
    }
}
