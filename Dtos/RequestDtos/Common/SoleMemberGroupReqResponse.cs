namespace NexgenCosysReport.Dtos.RequestDtos.Common
{

    public class SoleMemberGroupRequestDtos
    {
        public long lstOfficeId { get; set; }
    }
    public class SoleMemberGroupResponseDto
    {
        public long MemberGroupId { get; set; }
        public long UsmOfficeId { get; set; }
        public string? Name { get; set; }
    }
    public class SoleMemberGroupReqResponse
    {
    }
}
