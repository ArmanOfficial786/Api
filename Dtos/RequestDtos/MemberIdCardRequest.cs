namespace JsSampleReport.Dtos.RequestDtos
{
    public class MemberIdCardRequest
    {
        public string? fromDate { get; set; }
        public string? memberId { get; set; }
        public string? toDate { get; set; }
        public long branchId { get; set; }
        public long memberGroupId { get; set; }
        public int currentPage { get; set; } = 0;
        public int pageSize { get; set; } = 0;
        
    }
    public class MemberIdCardResponseModel
    {
        public long MemMemberRegistrationId { get; set; }

        public string? MemberId { get; set; }

        public string? Name { get; set; }

        public string? DateOfBirth { get; set; }

        public string? TemporaryAddress { get; set; }

        public string? PhoneNo { get; set; }

        public string? CitizenshipNo { get; set; }

        public string? RegistrationDate { get; set; }

        public string? Sex { get; set; }

        public string? MemberGroupName { get; set; }

        public DateTime RegistrationOn { get; set; }

        public string? FatherName { get; set; }

        public int EvenOdd { get; set; }

        public string? MemberPhoto { get; set; }
    }
}
