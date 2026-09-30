namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports
{
    public class CollectorWiseProjectionRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    public class CollectorWiseProjectionRowDto
    {
        public string? HurCollectorName { get; set; }
        public long? HurCollectorId { get; set; }

        public int? MemberProjection { get; set; }
        public decimal? LoanProjection { get; set; }
        public int? MUptoLstMProjected { get; set; }
        public int? MUptoLstMAdd { get; set; }
        public int? MUptoLstMRemove { get; set; }
        public int? MThisMProjected { get; set; }
        public int? MThisMAdd { get; set; }
        public int? MThisMRemove { get; set; }
        public int? MUptoThisMProjected { get; set; }
        public int? MUptoThisMAdd { get; set; }
        public int? MUptoThisMRemove { get; set; }
        public decimal? LUptoLstMProjected { get; set; }
        public decimal? LUptoLstMAdd { get; set; }
        public decimal? LThisMProjected { get; set; }
        public decimal? LThisMAdd { get; set; }
        public decimal? LUptoThisMProjected { get; set; }
        public decimal? LUptoThisMAdd { get; set; }
        public decimal? Progress { get; set; }
        public decimal? BalanceLoan { get; set; }

        public decimal? SavingProjection { get; set; }
        public decimal? ShareProjection { get; set; }
        public decimal? SaUptoLstMProjected { get; set; }
        public decimal? SaUptoLstMAdd { get; set; }
        public decimal? SaThisMProjected { get; set; }
        public decimal? SaThisMAdd { get; set; }
        public decimal? SaUptoThisMProjected { get; set; }
        public decimal? SaUptoThisMAdd { get; set; }
        public decimal? ShUptoLstMProjected { get; set; }
        public decimal? ShUptoLstMAdd { get; set; }
        public decimal? ShThisMProjected { get; set; }
        public decimal? ShThisMAdd { get; set; }
        public decimal? ShUptoThisMProjected { get; set; }
        public decimal? ShUptoThisMAdd { get; set; }
        public decimal? BalanceSaving { get; set; }
        public decimal? BalanceShare { get; set; }


        public int? TotalCenter { get; set; }
        public int? TotalMember { get; set; }
    }

    public class CollectorWiseProjectionData
    {
        public List<CollectorWiseProjectionRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }

        public int TotalMemberProjection { get; set; }
        public int TotalMThisMAdd { get; set; }
        public int TotalMUptoThisMAdd { get; set; }
        public int TotalMUptoThisMRemove { get; set; }
        public decimal TotalLoanProjection { get; set; }
        public decimal TotalLUptoThisMAdd { get; set; }
        public decimal TotalLUptoThisMProjected { get; set; }
        public decimal TotalBalanceLoan { get; set; }
        public int TotalCenters { get; set; }
        public int TotalMembers { get; set; }

        public string? FiscalYear { get; set; }
        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? MonthCode { get; set; }
        public string? PreviousMonthStartDate { get; set; }
        public string? PreviousMonthLastDate { get; set; }
        public string? CurrentMonthStartDate { get; set; }
        public string? CurrentFiscalYearStartDate { get; set; }
        public string? BranchName { get; set; }
    }
}