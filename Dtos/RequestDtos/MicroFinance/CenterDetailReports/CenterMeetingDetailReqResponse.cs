// Dtos/RequestDtos/Microfinance/CenterDetailReports/CenterMeetingDetailRequestDto.cs
namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports
{
    public class CenterMeetingDetailRequestDto
    {
        public string FromDateBs { get; set; } = string.Empty;
        public string ToDateBs { get; set; } = string.Empty;
        public string? BranchId { get; set; } = "-1";
        public string? CollectionCenterId { get; set; } = "-1";
        public string MeetingType { get; set; } = "rbNext";
        public bool VisualReport { get; set; } = false;
    }

    public class CenterMeetingDetailRowDto
    {
        public string? OfficeName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? CenterCode { get; set; }
        public string? MeetingDateBs { get; set; }
        public string? MeetingDateAd { get; set; }
        public string? MeetingTime { get; set; }
        public string? MeetingDay { get; set; }
        public string? CollectorName { get; set; }
        public string? GroupName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        public string? MeetingStatus { get; set; }
        public string? Remarks { get; set; }
        public string? CenterName { get; set; }
        public string? FieldVisitorName { get; set; }
        public string? VisitDateBs { get; set; }
        public string? VisitStatus { get; set; }
    }

    public class CenterMeetingDetailData
    {
        public List<CenterMeetingDetailRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public string? FromDateBs { get; set; }
        public string? ToDateBs { get; set; }
        public string? FromDateAd { get; set; }
        public string? ToDateAd { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? MeetingType { get; set; }
        public string? MeetingTypeName { get; set; }
    }
}