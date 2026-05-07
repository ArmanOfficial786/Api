namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class MemberGroupReqResponse
    {

    }
    public class MemberGroupRequestDtos
    {
        public long lstOfficeId { get; set; }
        public long CollectionCenterId { get; set; }
    }
    public class MemberGroupResponseDto
    {
        public long MemberGroupId { get; set; }
        public long CollectionCenterId { get; set; }
        public long UsmOfficeId { get; set; }
        public string? Name { get; set; }
    }
}
