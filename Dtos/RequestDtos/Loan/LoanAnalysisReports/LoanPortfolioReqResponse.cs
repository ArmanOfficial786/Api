
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanPortfolioRequestDto
    {
        public string? BranchIds { get; set; }
        public bool VisualReport { get; set; } = false;
    }

    public class LoanPortfolioParticularsDto
    {
        public string? Particulars { get; set; }
        public int? NoofLoan { get; set; }
        public decimal? LoanIssueAmount { get; set; }
    }

    public class LoanPortfolioOutstandingDto
    {
        public string? Particulars { get; set; }
        public int? NoofLoan { get; set; }
        public decimal? LoanIssueAmount { get; set; }
    }

    public class LoanPortfolioLoanTypeWiseDto
    {
        public string? LoanTypeName { get; set; }
        public int? NoofLoan { get; set; }
        public decimal? LoanIssueAmount { get; set; }
    }

    public class LoanPortfolioData
    {
        public List<LoanPortfolioParticularsDto> Particulars { get; set; } = [];
        public List<LoanPortfolioOutstandingDto> Outstanding { get; set; } = [];
        public List<LoanPortfolioLoanTypeWiseDto> LoanTypeWise { get; set; } = [];
        public int TotalRecords { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public int TotalLoans { get; set; }
        public string? BranchName { get; set; }
    }
}