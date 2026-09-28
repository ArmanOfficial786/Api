namespace NexgenCosysReport.Dtos.RequestDtos.Asset
{
    public class FamDetailReportRequestDto
    {
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long CategoryId { get; set; } = -1;
        public long AssetId { get; set; } = -1;
        public long ClearanceStatusId { get; set; } = -1;
        public string ReportType { get; set; } = "ASSET";
        public bool IsTypeWise { get; set; } = false;
        public bool IsSummary { get; set; } = true;
        public string OrderBy { get; set; } = "";
        public bool VisualReport { get; set; } = false;
    }

    public class FamDetailReportRowDto
    {
        public string? Code { get; set; }
        public string? FixedAssetsName { get; set; }
        public string? TransactionType { get; set; }
        public decimal? Amount { get; set; }
        public string? TransactionDateBS { get; set; }
        public string? PurchaseStatus { get; set; }
        public string? VerifiedTo { get; set; }
        public string? TransferredOut { get; set; }
        public string? TransferredIn { get; set; }
        public string? Clearance { get; set; }
    }

    public class FamDetailReportSummaryDto
    {
        public string? FixedAssetsName { get; set; }
        public int? PurchaseStatusQty { get; set; }
        public int? VerifiedToQty { get; set; }
        public int? TransferredOutQty { get; set; }
        public int? TransferredInQty { get; set; }
        public int? ClearanceQty { get; set; }
        public int? Balance { get; set; }
    }

    public class FamDetailReportData
    {
        public List<FamDetailReportRowDto> Rows { get; set; } = [];
        public List<FamDetailReportSummaryDto> Summary { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? CategoryName { get; set; }
        public string? AssetName { get; set; }
        public string? ReportType { get; set; }
        public bool IsTypeWise { get; set; }
        public bool IsSummary { get; set; }
        public string? OrderBy { get; set; }
    }
}
