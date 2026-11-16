//namespace NexgenCosysReport.Dtos.RequestDtos.Common
//{
//    public class MemberLookUpDtos
//    {
//        public long MemMemberRegistrationId { get; set; }
//        public string? CenterName { get; set; }
//        public string? CenterCode { get; set; }          // CollectionCenterShortCode
//        public string? GroupName { get; set; }
//        public string? GroupCode { get; set; }           // GroupShortCode
//        public string? OfficeName { get; set; }
//        public string? MemberId { get; set; }
//        public string? MemberName { get; set; }
//        public string? Gender { get; set; }
//        public string? TemporaryAddress { get; set; }
//        public string? MobileNo { get; set; }

//        // ? Pagination metadata — mapped directly from SP columns
//        public int TotalCount { get; set; }
//        public int CurrentPage { get; set; }
//        public int PageSize { get; set; }
//        public int TotalPages { get; set; }
//    }

//    // Returned when user clicks "Sel" button
//    public class MemberSelectedDto
//    {
//        //public long MemMemberRegistrationId { get; set; }
//        public string? MemberId { get; set; }
//        public string? MemberName { get; set; }
//        //public string? CenterName { get; set; }
//        //public string? CenterCode { get; set; }
//        //public string? GroupName { get; set; }
//        //public string? GroupCode { get; set; }
//        //public string? OfficeName { get; set; }
//        //public string? Gender { get; set; }
//        //public string? TemporaryAddress { get; set; }
//        //public string? MobileNo { get; set; }
//    }
//}



namespace NexgenCosysReport.Dtos.RequestDtos.Common
{
    public class MemberLookUpDtos
    {
        public long MemMemberRegistrationId { get; set; }
        public string? CenterName { get; set; }
        public string? CenterCode { get; set; }          // CollectionCenterShortCode
        public string? GroupName { get; set; }
        public string? GroupCode { get; set; }           // GroupShortCode
        public string? OfficeName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Gender { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? MobileNo { get; set; }
    }

    // Returned when user clicks "Sel" button
    public class MemberSelectedDto
    {
        public long MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
    }

    // Mirrors the SP result columns exactly so Dapper maps them by name
    public class MemberSpRow
    {
        public long MemMemberRegistrationId { get; set; }
        public string? GroupName { get; set; }
        public string? CenterName { get; set; }
        public string? CollectionCenterShortCode { get; set; }
        public string? GroupShortCode { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Gender { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? MobileNo { get; set; }
        public string? OfficeName { get; set; }
    }
}
