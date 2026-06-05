namespace NexgenCosysReport.Dtos.RequestDtos.Member
{
    public class MemberDetailRequest
    {
        public string? fromDate { get; set; }
        public string? toDate { get; set; }
        public string? memberId { get; set; }
        public long branchId { get; set; }
        public long memberGroupId { get; set; }
        public string? orderby { get; set; }
        public bool VisualReport { get; set; } = false;
    }
}
