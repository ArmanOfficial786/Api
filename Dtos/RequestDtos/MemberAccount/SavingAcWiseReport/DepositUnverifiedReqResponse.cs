namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.SavingAcWiseReport
{
    public class DepositUnverifiedRequest
    {
        public string? FromDate { get; set; }          // Nepali "yyyy/MM/dd"
        public string? ToDate { get; set; }
        public string? MemberId { get; set; }           // Member ID string
        public string? MemberName { get; set; }
        public string? BranchIds { get; set; }          // comma-separated
        public string BranchName { get; set; } = "All";
        public string? DepositTypeId { get; set; }
        public string? CollectorId { get; set; }
        public string OrderBy { get; set; } = "MemberId";
        public string ReportType { get; set; } = "A";   // A=All, V=Verified, U=Unverified
        public bool SameCompanyName { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }


    public class DepositUnverifiedRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositTypeName { get; set; }
        public string? AccountOpenDate { get; set; }   // bound from SP's "AccountOpenOnBs"
        public string? CollectorName { get; set; }     // bound from SP's "Collector"
        public string? VerifiedTill { get; set; }
        public string? VerifiedDate { get; set; }
        public string? VerifiedBy { get; set; }
        public bool IsVerified { get; set; }            // set in code, not bound from SQL
        public decimal Balance { get; set; }
        public string? Status { get; set; }
    }

    public class DepositUnverifiedData
    {
        public List<DepositUnverifiedRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public int VerifiedCount { get; set; }
        public int UnverifiedCount { get; set; }
    }

    public class DepositUnverifiedReqResponse
    {
    }
}
