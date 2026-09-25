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

        public string? Address { get; set; }
        public string? MeetingDateBS { get; set; }
        public string? MeetingTime { get; set; }

        // Combination of meeting date (BS) and meeting time
        public string? MeetingDateTimeBS =>
            $"{MeetingDateBS} {MeetingTime}";
    }
}
