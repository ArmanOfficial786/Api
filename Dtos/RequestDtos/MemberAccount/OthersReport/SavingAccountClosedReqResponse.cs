namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{

    public class SavingAccountClosedRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
        public long? MemberId { get; set; } = -1;
        public string ReportMode { get; set; } = "DateWise"; // DateWise, MemberWise
    }

    public class SavingAccountClosedRowDto
    {
        public string? MemberId { get; set; }
        public string? Name { get; set; }              // was MemberName — SP column is "Name"
        public string? AccountNo { get; set; }
        public string? OpenedDate { get; set; }         // was AccountOpenOnBs — SP column is "OpenedDate"
        public string? DepositTypeName { get; set; }
        public string? ClosedDate { get; set; }         // was AccountCloseOnBs — SP column is "ClosedDate"
        public decimal? CloseAmount { get; set; }
        public decimal? ChargeAmount { get; set; }      // was Charge — SP column is "ChargeAmount"
        public decimal? NetAmount { get; set; }
        public string? Operator { get; set; }           // was CollectorName — SP column is "Operator" (FullName of the charge transaction's creator, not a collector)
    }

    public class SavingAccountClosedData
    {
        public List<SavingAccountClosedRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalCloseAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportMode { get; set; }
        public string? SelectedMemberId { get; set; }
        public string? SelectedMemberName { get; set; }
        public int TotalClosedAccounts { get; set; }
    }
    public class SavingAccountClosedReqResponse
    {
    }
}
