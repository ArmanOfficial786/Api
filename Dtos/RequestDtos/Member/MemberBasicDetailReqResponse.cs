namespace NexgenCosysReport.Dtos.RequestDtos.Member
{
    public class MemberBasicDetailsRequest
    {
        public long MemberRegistrationId { get; set; }
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public string? BranchIds { get; set; }
        public string? OrderBy { get; set; }
        public bool VisualReport { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public string? BranchSelected { get; set; }
        public string? BranchName { get; set; }
    }
    public class MemberBasicDetailsSpDto
    {
        public long MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public string? PAddress { get; set; }
        // Nepali date
        public string? ContactNo { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? DateOfBirth { get; set; }
        public int? NoOfYear { get; set; }           // age in years
        public int? NoOfDay { get; set; }            // age in days (optional)
    }
    public class MemberBasicDetailReqResponse
    {
    }
}
