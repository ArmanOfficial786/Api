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
        public string? DateOfBirth { get; set; }   // Nepali date
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? PhoneNo { get; set; }
        public string? MobileNo { get; set; }
        public string? EmailId { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? PassportNo { get; set; }
        public string? GrandFatherName { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? RegistrationDate { get; set; }
        public string? Nationality { get; set; }
        public string? Sex { get; set; }
        public string? VDC { get; set; }
        public string? District { get; set; }
        public string? Zone { get; set; }
        public string? Religion { get; set; }
        public string? Caste { get; set; }
        public string? Occupation { get; set; }
        public int? NoOfYear { get; set; }           // age in years
        public int? NoOfMonth { get; set; }          // age in months (optional)
        public int? NoOfDay { get; set; }            // age in days (optional)
    }
    public class MemberBasicDetailReqResponse
    {
    }
}
