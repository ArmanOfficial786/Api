namespace NexgenCosysReport.Dtos.RequestDtos
{
    public class MemberDetailRequest
    {
        public string? fromDate { get; set; }
        public string? toDate { get; set; }
        public long branchId { get; set; }
        public long memberGroupId { get; set; }
        public int currentPage { get; set; } = 0;
        public int pageSize { get; set; } = 0;

    }
}
