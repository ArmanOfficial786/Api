namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class CollectionCenterReqResponseDtos
    {

    }
    public class CollectionCenterRequestDtos
    {
        public long LstOfficeId { get; set; }
    }

    public class CollectionCenterResponseDto
    {
        public long CollectionCenterId { get; set; }
        public string? CollectionCenterShortCode { get; set; }
        public string? CollectionCenterName { get; set; }
    }
}
