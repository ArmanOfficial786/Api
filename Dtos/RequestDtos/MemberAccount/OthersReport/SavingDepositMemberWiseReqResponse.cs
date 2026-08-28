namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.OthersReport
{


    public class SavingDepositMemberWiseRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string BranchIds { get; set; } = "-1";
        public string OrderBy { get; set; } = "Member Id";
        public string TransactionType { get; set; } = "Deposit"; // Deposit, Withdrawl
        public long? MemberId { get; set; } = -1;
        public long? SavingTypeId { get; set; } = -1;
        public string BranchName { get; set; } = "All Branches";
        public bool VisualReport { get; set; } = false;
        public string ReportMode { get; set; } = "DateWise"; // DateWise, MemberWise
    }

    public class SavingDepositMemberWiseRowDto
    {
        public string? Date { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? SavingType { get; set; }
        public decimal? Amount { get; set; }
        public string? Operator { get; set; }
        public string? Description { get; set; }
        public string? TransactionType { get; set; }
    }

    public class SavingDepositMemberWiseData
    {
        public List<SavingDepositMemberWiseRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? TransactionType { get; set; }
        public string? ReportMode { get; set; }
        public string? SelectedMemberId { get; set; }
        public string? SelectedMemberName { get; set; }
        public string? SavingTypeName { get; set; }
        public decimal TotalDepositAmount { get; set; }
        public decimal TotalWithdrawalAmount { get; set; }
    }
    public class SavingDepositMemberWiseReqResponse
    {
    }
}
