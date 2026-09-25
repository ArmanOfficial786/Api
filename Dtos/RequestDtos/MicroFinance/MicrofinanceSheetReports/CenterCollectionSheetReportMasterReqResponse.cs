// Dtos/RequestDtos/Microfinance/MicrofinanceSheetReports/CenterCollectionSheetReportMasterDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.MicrofinanceSheetReports
{
    public class CenterCollectionSheetReportMasterDto
    {
        public int SycCollectionCenterScemeId { get; set; }
        public string? SavingName { get; set; }
        public string? SavingNameCode { get; set; }
        public string? LoanName { get; set; }
        public string? LoanNameCode { get; set; }
    }

    public class CenterCollectionSheetReportMasterUpdateDto
    {
        public int SycCollectionCenterScemeId { get; set; }
        public string SavingName { get; set; } = string.Empty;
        public string SavingNameCode { get; set; } = string.Empty;
        public string LoanName { get; set; } = string.Empty;
        public string LoanNameCode { get; set; } = string.Empty;
    }

    public class CenterCollectionSheetReportMasterResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}