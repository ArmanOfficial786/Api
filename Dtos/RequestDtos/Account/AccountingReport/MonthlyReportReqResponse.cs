namespace NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport
{
    public class MonthlyReportRequest
    {
        public string TillDate { get; set; } = string.Empty;  // Nepali "yyyy/MM/dd"
        public string? BranchId { get; set; }
        public string BranchName { get; set; } = "All";
        public int AccountTypeId { get; set; } = -1;          // -1 = All
        public string ReportType { get; set; } = "Summary";              // 1=Summary, 2=SubLedger, 3=Detail
        public bool IsMonthWise { get; set; } = false;
        public bool IsNepali { get; set; } = false;
        public bool ShowBudget { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }
}

// Dtos/ReportDtos/MonthlyReportDto.cs
namespace NexgenCosysReport.Dtos.ReportDtos
{
    public class MonthlyReportRowDto
    {
        public string? LedgerHead { get; set; }
        public string? MainLedger { get; set; }
        public string? SubLedger { get; set; }
        public string? SubLedger1 { get; set; }
        public string? SubLedger2 { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal PreviousAmount { get; set; }
        public decimal BudgetAmount { get; set; }
    }

    public class MonthlyReportData
    {
        public List<MonthlyReportRowDto> AssetsRows { get; set; } = new();
        public List<MonthlyReportRowDto> LiabilitiesRows { get; set; } = new();
        public List<MonthlyReportRowDto> IncomeRows { get; set; } = new();
        public List<MonthlyReportRowDto> ExpensesRows { get; set; } = new();
        public decimal TotalAssets { get; set; }
        public decimal TotalLiabilities { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
    }
    public class MonthlyReportReqResponse
    {
    }
}
