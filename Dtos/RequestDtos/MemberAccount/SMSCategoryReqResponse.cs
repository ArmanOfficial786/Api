namespace NexgenCosysReport.Dtos.RequestDtos.MemberAccount
{
    public class SMSCategoryRequest
    {
        public string? BranchId { get; set; }
        public string BranchName { get; set; } = "All";
        public string? SmsCategoryId { get; set; }
        public string OrderBy { get; set; } = "Member ID";

        public bool VisualReport { get; set; } = false;
    }


    public class SMSCategoryRowDto
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? AccountNo { get; set; }
        public string? DepositTypeName { get; set; }
        public string? DateOfAccountOpen { get; set; }
        public string? SMSCriteria { get; set; }
        public string? SMSMessage { get; set; }
        public decimal Balance { get; set; }
    }

    public class SMSCategoryData
    {
        public List<SMSCategoryRowDto> Rows { get; set; } = new();
        public int TotalRecords { get; set; }
    }

    public class SMSCategoryReqResponse
    {
    }
}
