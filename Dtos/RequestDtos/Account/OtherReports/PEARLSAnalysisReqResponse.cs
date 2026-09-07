namespace NexgenCosysReport.Dtos.RequestDtos.Account.OtherReports
{
    public class PEARLSAnalysisRequestDto
    {
        public string TillDate { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public string OrderBy { get; set; } = "Title";
        public bool VisualReport { get; set; }
    }

    // Matches the SP output columns exactly, one row per indicator
    // (e.g. P, P2, P1, P6 under the "P = PROTECTION" section)
    public class PEARLSBalanceDto
    {
        public string? Indicator { get; set; }      // "P", "P2", "P1", "P6"...
        public string? Objective { get; set; }      // "Good Loans", "Dutiful Loans"...
        public string? Formula { get; set; }        // full formula text
        public string? Measurement { get; set; }    // "1%", "35%", "70-80%", ">5%"
        public decimal? Value { get; set; }         // 11.03, 0.27, 0.07...
        public string? Achievement { get; set; }    // "Monthly" (frequency, not a %)
    }

    public class PEARLSAnalysisData
    {
        public List<PEARLSBalanceDto> PEARLSP { get; set; } = new();
        public List<PEARLSBalanceDto> PEARLSE { get; set; } = new();
        public List<PEARLSBalanceDto> PEARLSA { get; set; } = new();
        public List<PEARLSBalanceDto> PEARLSR { get; set; } = new();
        public List<PEARLSBalanceDto> PEARLSL { get; set; } = new();
        public List<PEARLSBalanceDto> PEARLSS { get; set; } = new();
        public string? TillDate { get; set; }
        public string? BranchNames { get; set; }
        public string? FiscalYear { get; set; }
        public DateTime PreviousDate { get; set; }
    }

    public class PEARLSAnalysisReqResponse
    {
    }
}
