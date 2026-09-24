//// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanAgeingTypeWiseRequestDto.cs
//namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
//{
//    public class LoanAgeingTypeWiseRequestDto
//    {
//        public string TillDateBs { get; set; } = string.Empty;
//        public string? BranchIds { get; set; }
//        public string? MemberGroupId { get; set; } = "-1";
//        public string PenaltyType { get; set; } = "S";
//        public string OrderBy { get; set; } = "LoanTypeName";
//        public bool WithMemberCount { get; set; } = false;
//        public bool VisualReport { get; set; } = false;
//    }

//    public class LoanAgeingTypeWiseRowDto
//    {
//        public string? LoanTypeName { get; set; }
//        public int? NoofLoan { get; set; }
//        public decimal? LoanIssueAmount { get; set; }
//        public decimal? Repaid { get; set; }
//        public decimal? Overdue { get; set; }
//        public decimal? GoodLoan { get; set; }
//        public int? MemberCount { get; set; }
//    }

//    public class LoanAgeingTypeWiseData
//    {
//        public List<LoanAgeingTypeWiseRowDto> Rows { get; set; } = [];
//        public int TotalRecords { get; set; }
//        public int TotalLoans { get; set; }
//        public int TotalMembers { get; set; }
//        public decimal TotalLoanIssueAmount { get; set; }
//        public decimal TotalRepaid { get; set; }
//        public decimal TotalOverdue { get; set; }
//        public decimal TotalGoodLoan { get; set; }
//        public string? TillDateBs { get; set; }
//        public string? BranchName { get; set; }
//        public string? MemberGroupName { get; set; }
//        public string? PenaltyType { get; set; }
//        public string? PenaltyTypeName { get; set; }
//        public string? OrderBy { get; set; }
//        public bool WithMemberCount { get; set; }
//    }
//}







// Dtos/RequestDtos/Loan/LoanAnalysisReport/LoanAgeingTypeWiseRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Loan.LoanAnalysisReport
{
    public class LoanAgeingTypeWiseRequestDto
    {
        public string TillDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? MemberGroupId { get; set; } = "-1";
        public string PenaltyType { get; set; } = "S";
        public string OrderBy { get; set; } = "LoanTypeName";
        public bool WithMemberCount { get; set; } = false;
        public bool VisualReport { get; set; } = false;
    }

    // Property names match the literal columns both stored procedures
    // (sp_7_16_LoanAgeingTypeWiseReport and ...WithMemberCountReport) return —
    // not friendlier renamed versions — so Dapper's name-based binding
    // actually populates them. In particular:
    //   - "LoanAccountNo" here is a loan COUNT (count(LoanTypeName) as
    //     LoanAccountNo in the SP), not an account number string.
    //   - "TotalPrincipalAmount" is the image's "Balance Amt" column.
    //   - "NoOfMemMember" and "GoodLoan" are present on both SPs (GoodLoan
    //     is always 0/absent on the plain variant, since that SP never
    //     computes it — matches the image, which has no member-count/good-
    //     loan columns).
    public class LoanAgeingTypeWiseRowDto
    {
        public string? LoanTypeName { get; set; }
        public int? NoOfMemMember { get; set; }
        public int? LoanAccountNo { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public decimal? Repaid { get; set; }
        public decimal? TotalPrincipalAmount { get; set; }
        public decimal? Overdue { get; set; }
        public decimal? GoodLoan { get; set; }
    }

    public class LoanAgeingTypeWiseData
    {
        public List<LoanAgeingTypeWiseRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalLoans { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public decimal TotalRepaid { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal TotalOverdue { get; set; }
        public decimal TotalGoodLoan { get; set; }
        public string? TillDateBs { get; set; }
        public string? TillDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? PenaltyType { get; set; }
        public string? PenaltyTypeName { get; set; }
        public string? OrderBy { get; set; }
        public bool WithMemberCount { get; set; }
    }
}