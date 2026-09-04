namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount.InterestPayableReport
{
    public class SavingsAccountMaturityRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchIds { get; set; }
        public string? BranchName { get; set; }
        public long DepositTypeId { get; set; } = -1;
        public string OrderBy { get; set; } = "Member Id";
        public bool VisualReport { get; set; } = false;
        public string Format { get; set; } = "VIEW";
    }

    public class SavingsAccountMaturityRowDto
    {
        public string? DepositTypeName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        /// <summary>
        /// Contact number(s) shown in the "Member Details" column, e.g.
        /// ";9856328547" or "982323173;9808196158" (office;mobile —
        /// rendered exactly as the SP returns it, semicolon and all).
        /// </summary>
        public string? Phone { get; set; }

        public string? AccountNo { get; set; }
        public string? AccountOpenOnBs { get; set; }
        public string? MaturityOnBs { get; set; }
        public decimal? Balance { get; set; }
        public decimal? InterestRate { get; set; }
        public decimal? DepositAmount { get; set; }
        public decimal? MaturityAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class SavingsAccountMaturityData
    {
        public List<SavingsAccountMaturityRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
        public decimal TotalDepositAmount { get; set; }
        public decimal TotalMaturityAmount { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? BranchNames { get; set; }
        public string? OrderBy { get; set; }
        public string? DepositTypeName { get; set; }
        public int TotalDepositTypes { get; set; }
    }
    public class SavingsAccountMaturityReqResponse
    {
    }
}
