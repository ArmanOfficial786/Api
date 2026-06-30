namespace NexgenCosysReport.Dtos.RequestDtos.Member
{
    public class MemberBloodGroupReportRequest
    {
        public string? FromDate { get; set; }
        public string? ToDate { get; set; }
        public long BranchId { get; set; }
        public long MemberGroupId { get; set; }
        public long BloodGroupOption { get; set; }
        public string? OrderBy { get; set; }
        public bool VisualReport { get; set; } = false;
        public bool SameCompanyName { get; set; } = true;
        public string? BranchSelected { get; set; }
        public string? BranchName { get; set; }
    }

    public class MemberBloodGroupSpDto
    {
        public string? MemberId { get; set; }
        public string? Name { get; set; }
        public string? Sex { get; set; }
        public string? DateOfBirth { get; set; }   // Nepali
        public string? RegistrationDate { get; set; }
        public string? BloodGroup { get; set; }
        public string? ContactNo { get; set; }
        public string? Address { get; set; }
        // Add other fields as needed
    }

    public class MemberBloodGroupReqResponse
    {
    }
}
