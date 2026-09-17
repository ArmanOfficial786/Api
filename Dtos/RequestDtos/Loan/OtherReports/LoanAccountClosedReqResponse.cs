//namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
//{
//    public class LoanAccountClosedRequestDto
//    {
//        public string? FromDateBs { get; set; }
//        public string? ToDateBs { get; set; }
//        public string? BranchIds { get; set; }
//        public long MemberGroupId { get; set; } = -1;
//        public string OrderBy { get; set; } = "MemberId";
//        public bool VisualReport { get; set; } = false;
//    }
//    public class LoanAccountClosedRowDto
//    {
//        public long? LmtLoanIssueId { get; set; }
//        public string? MemberId { get; set; }
//        public string? MemberIdFirst { get; set; }
//        public decimal? MemberIdLast { get; set; }
//        public string? FullName { get; set; }
//        public string? LoanAccountNo { get; set; }
//        public string? LoanTypeName { get; set; }
//        public decimal? LoanIssueAmount { get; set; }
//        public string? LoanIssueDate { get; set; }
//        public string? LoanCloseDate { get; set; }
//        public decimal? LoanCloseAmount { get; set; }
//        public string? MobileNo { get; set; }
//        public string? TemporaryAddressDetail { get; set; }
//        public string? CollectionCenterName { get; set; }
//    }

//    public class LoanAccountClosedData
//    {
//        public List<LoanAccountClosedRowDto> Rows { get; set; } = [];
//        public int TotalRecords { get; set; }
//        public decimal TotalLoanIssueAmount { get; set; }
//        public decimal TotalLoanCloseAmount { get; set; }
//        public string? FromDateBs { get; set; }
//        public string? ToDateBs { get; set; }
//        public string? BranchName { get; set; }
//        public string? MemberGroupName { get; set; }
//        public string? OrderBy { get; set; }
//    }
//}




namespace NexgenCosysReport.Dtos.RequestDtos.Loan.OtherReports
{
    public class LoanAccountClosedRequestDto
    {
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchIds { get; set; }
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberId";
        public bool VisualReport { get; set; } = false;
    }

    // --------------------------------------------------------------------
    // Matches sp_7_16_LoanAccountClosedReport's SELECT list exactly.
    // The SP aliases the close date as "CloseDate", not "LoanCloseDate" -
    // that mismatch is why Close Date always rendered empty. LoanCloseAmount,
    // MobileNo, TemporaryAddressDetail and CollectionCenterName are removed
    // because the SP never selects them - they were always null.
    // --------------------------------------------------------------------
    public class LoanAccountClosedRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberIdFirst { get; set; }
        public decimal? MemberIdLast { get; set; }
        public string? FullName { get; set; }
        public string? LoanAccountNo { get; set; }
        public string? LoanTypeName { get; set; }
        public decimal? LoanIssueAmount { get; set; }
        public string? LoanIssueDate { get; set; }
        public string? CloseDate { get; set; } // was "LoanCloseDate" - renamed to match the SP's column alias
    }

    public class LoanAccountClosedData
    {
        public List<LoanAccountClosedRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalLoanIssueAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
    }
}