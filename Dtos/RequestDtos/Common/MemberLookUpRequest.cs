namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class MemberLookUpRequest
    {
        public int Page { get; set; } = 1;          // page number, default 1

        // Optional column filters (same as stored procedure)
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? GroupName { get; set; }
        public string? CenterName { get; set; }
        public string? Gender { get; set; }
        public string? MobileNo { get; set; }
        public string? OfficeName { get; set; }
        public string? GroupCode { get; set; }
        public string? CenterCode { get; set; }
        // Add any other filter columns you need

        // ? Sorting — were missing, caused the errors
        public string SortColumn { get; set; } = "MemberId";
        public string SortDirection { get; set; } = "ASC";
    }
}
