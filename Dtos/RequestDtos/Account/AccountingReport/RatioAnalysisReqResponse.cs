namespace NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport
{
    public class RatioAnalysisRequest
    {
        public string FromDate { get; set; } = string.Empty;  // Nepali "yyyy/MM/dd"
        public string ToDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }                // comma‑separated
        public string BranchName { get; set; } = "All";
        public string ProvisionType { get; set; } = "S";      // S=Schedule Wise, R=Remaining Principal, A=After Maturity
        public bool Enable1to30Days { get; set; } = false;
        public bool IsTotalOnly { get; set; } = false;        // true = Total Only, false = Detail
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class RatioAnalysisRowDto
    {
        public string? Category { get; set; }        // e.g., "Liquidity Ratios"
        public string? RatioName { get; set; }
        public decimal Value { get; set; }
    }

    public class RatioAnalysisData
    {
        public List<RatioAnalysisRowDto> Rows { get; set; } = new();
    }
    public class RatioAnalysisReqResponse
    {
    }
}
