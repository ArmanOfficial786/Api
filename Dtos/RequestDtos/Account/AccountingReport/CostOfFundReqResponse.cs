namespace NexgenCosysReport.Dtos.RequestDtos.Account.AccountingReport
{
    public class CostOfFundRequest
    {
        public string? TillDate { get; set; } = string.Empty;
        public long BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? OrderBy { get; set; }
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class CostOfFundRowDto
    {
        public string? TypeName { get; set; }
        public int NoOfAccount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageInterestRate { get; set; }
        public decimal WACC { get; set; }
    }

    public class CostOfFundData
    {
        public List<CostOfFundRowDto> DepositRows { get; set; } = new();
        public List<CostOfFundRowDto> LoanRows { get; set; } = new();
        public decimal CostOfDeposit { get; set; }
        public decimal CostOfLoan { get; set; }
        public decimal CostOfDepositInterest { get; set; }
        public decimal CostOfLoanInterest { get; set; }
        public decimal CRR { get; set; }   // from additional details
        public decimal SLR { get; set; }
        public decimal CDR { get; set; }
    }
    public class CostOfFundReqResponse
    {
    }
}
