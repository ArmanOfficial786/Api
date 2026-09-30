//// Dtos/RequestDtos/Microfinance/CenterDetailReports/CenterMeetingDetailRequestDto.cs
//namespace NexgenCosysReport.Dtos.RequestDtos.Microfinance.CenterDetailReports
//{
//    public class CenterMeetingDetailRequestDto
//    {
//        public string FromDateBs { get; set; } = string.Empty;
//        public string ToDateBs { get; set; } = string.Empty;
//        public string? BranchId { get; set; } = "-1";
//        public string? CollectionCenterId { get; set; } = "-1";
//        public string MeetingType { get; set; } = "rbNext";
//        public bool VisualReport { get; set; } = false;
//    }

//    public class CenterMeetingDetailRowDto
//    {
//        public string? OfficeName { get; set; }
//        public string? CollectionCenterName { get; set; }
//        public string? CenterCode { get; set; }
//        public string? MeetingDateBs { get; set; }
//        public string? MeetingDateAd { get; set; }
//        public string? MeetingTime { get; set; }
//        public string? MeetingDay { get; set; }
//        public string? CollectorName { get; set; }
//        public string? GroupName { get; set; }
//        public string? MemberId { get; set; }
//        public string? MemberName { get; set; }
//        public string? ContactNo { get; set; }
//        public string? Address { get; set; }
//        public string? MeetingStatus { get; set; }
//        public string? Remarks { get; set; }
//        public string? CenterName { get; set; }
//        public string? FieldVisitorName { get; set; }
//        public string? VisitDateBs { get; set; }
//        public string? VisitStatus { get; set; }
//    }

//    public class CenterMeetingDetailData
//    {
//        public List<CenterMeetingDetailRowDto> Rows { get; set; } = [];
//        public int TotalRecords { get; set; }
//        public string? FromDateBs { get; set; }
//        public string? ToDateBs { get; set; }
//        public string? FromDateAd { get; set; }
//        public string? ToDateAd { get; set; }
//        public string? BranchName { get; set; }
//        public string? CollectionCenterName { get; set; }
//        public string? MeetingType { get; set; }
//        public string? MeetingTypeName { get; set; }
//    }
//}






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

    // Matches sp_4_113_GetCenterMeetingDetailReport's actual SELECT list exactly:
    // CenterCode, CenterName, MeetingDate, ChangedDate, CollectorName
    public class CenterMeetingDetailRowDto
    {
        public string? CenterCode { get; set; }
        public string? CenterName { get; set; }
        public string? MeetingDate { get; set; }     // pre-formatted "yyyy/MM/dd, hh:mm AM/PM Dayname" by the SP
        public string? ChangedDate { get; set; }     // same format, empty when no change recorded
        public string? CollectorName { get; set; }
    }

    // Kept separate for the rbDetail (field visit schedule) path — that SP returns a
    // pivoted per-day/per-collector shape (#tblSchedule + a second Collector1..8 header
    // row), not a flat row list. It needs its own DTO and view once that report's
    // expected layout is confirmed; using CenterMeetingDetailRowDto for it would be
    // just as wrong as it was here.
    public class CenterFieldVisitScheduleRowDto
    {
        public DateTime? DateAD { get; set; }
        public string? DateBS { get; set; }
        public string? Day { get; set; }
        public string? Collector1 { get; set; }
        public string? Collector2 { get; set; }
        public string? Collector3 { get; set; }
        public string? Collector4 { get; set; }
        public string? Collector5 { get; set; }
        public string? Collector6 { get; set; }
        public string? Collector7 { get; set; }
        public string? Collector8 { get; set; }
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