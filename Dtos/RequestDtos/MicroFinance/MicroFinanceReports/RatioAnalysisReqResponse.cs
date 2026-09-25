// Dtos/RequestDtos/Microfinance/MicrofinanceReport/RatioAnalysisRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.MicroFinance.MicroFinanceReports
{
    public class RatioAnalysisRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public bool Enable1To30Days { get; set; } = false;
        public string ProvisionType { get; set; } = "S";
        public string ViewMode { get; set; } = "D";
        public bool VisualReport { get; set; } = false;
    }

    public class RatioAnalysisRowDto
    {
        public int GroupOrder { get; set; }
        public string? GroupName { get; set; }
        public int SN { get; set; }
        public string? Detail { get; set; }
        public decimal? Total { get; set; }
        public decimal? TotalPercent { get; set; }
        public decimal? Col1 { get; set; }
        public decimal? ColPer1 { get; set; }
        public decimal? Col2 { get; set; }
        public decimal? ColPer2 { get; set; }
        public decimal? Col3 { get; set; }
        public decimal? ColPer3 { get; set; }
        public decimal? Col4 { get; set; }
        public decimal? ColPer4 { get; set; }
        public decimal? Col5 { get; set; }
        public decimal? ColPer5 { get; set; }
        public decimal? Col6 { get; set; }
        public decimal? ColPer6 { get; set; }
        public decimal? Col7 { get; set; }
        public decimal? ColPer7 { get; set; }
        public decimal? Col8 { get; set; }
        public decimal? ColPer8 { get; set; }
        public decimal? Col9 { get; set; }
        public decimal? ColPer9 { get; set; }
        public decimal? Col10 { get; set; }
        public decimal? ColPer10 { get; set; }
        public decimal? Col11 { get; set; }
        public decimal? ColPer11 { get; set; }
        public decimal? Col12 { get; set; }
        public decimal? ColPer12 { get; set; }
        public decimal? Col13 { get; set; }
        public decimal? ColPer13 { get; set; }
        public decimal? Col14 { get; set; }
        public decimal? ColPer14 { get; set; }
        public decimal? Col15 { get; set; }
        public decimal? ColPer15 { get; set; }
    }

    public class RatioAnalysisBranchDto
    {
        public string? OfficeName { get; set; }
    }

    public class RatioAnalysisGroupDto
    {
        public int GroupOrder { get; set; }
        public string? GroupName { get; set; }
        public List<RatioAnalysisRowDto> Rows { get; set; } = [];
    }

    public class RatioAnalysisData
    {
        public List<RatioAnalysisGroupDto> Groups { get; set; } = [];
        public List<RatioAnalysisBranchDto> Branches { get; set; } = [];
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? FromDateAd { get; set; }
        public string? ToDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? ProvisionType { get; set; }
        public string? ProvisionTypeName { get; set; }
        public string? ViewMode { get; set; }
        public string? ViewModeName { get; set; }
        public bool Enable1To30Days { get; set; }
    }
}