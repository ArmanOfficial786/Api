namespace NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport
{
    public class OfficeProgressRequest
    {
        public string TillDate { get; set; } = string.Empty;   // Nepali "yyyy/MM/dd"
        public string? BranchId { get; set; }                 // comma-separated
        public string BranchName { get; set; } = "All";
        public string ReportType { get; set; } = "Office Progress"; // Office Progress, Saving, Loan
        public bool Enable1to30Days { get; set; } = false;
        public string ProvisionType { get; set; } = "S";       // S=Schedule Wise, R=Remaining Principal, A=After Maturity
        public bool GroupByBranch { get; set; } = false;
        public bool GroupByCollectionCenter { get; set; } = false;
        public bool ViewDetail { get; set; } = true;
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class OfficeProgressRowDto
    {
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? GroupName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalDeposit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalLoan { get; set; }
        public decimal TotalSaving { get; set; }
        public decimal TotalShare { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetProfit { get; set; }
        // Additional fields as needed
    }

    public class OfficeProgressData
    {
        public List<OfficeProgressRowDto> Rows { get; set; } = new();
        public decimal GrandTotal { get; set; }
    }
    public class OfficeProgressReqResponse
    {
    }
}
