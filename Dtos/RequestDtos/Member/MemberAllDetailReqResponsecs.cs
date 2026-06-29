namespace NexgenCosysReport.Dtos.RequestDtos.Member
{
    public class MemberAllDetailReqResponsecs
    {
    }

    public class MemberAllDetailRequst
    {
        public string? fromDate { get; set; }
        public string? toDate { get; set; }
        public string? memberId { get; set; }
        public string? orderby { get; set; }
        public long memberGroupId { get; set; }
        public long branchId { get; set; }
        public bool visualReport { get; set; }

        // Column keys to display. Null or empty => show all columns (default-checked state).
        public List<string>? SelectedColumns { get; set; }
    }

    public class MemberAllDetailSpResponse
    {
        public long MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? MemberType { get; set; }
        public string? BirthOnBS { get; set; }
        public string? PermanentAddress { get; set; }
        public string? TemporaryAddress { get; set; }
        public string? PhoneNo { get; set; }
        public string? MobileNo { get; set; }
        public string? EmailAddress { get; set; }
        public string? Nationality { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? PassportNo { get; set; }
        public string? Occupation { get; set; }
        public string? Religion { get; set; }
        public string? MaritalStatus { get; set; }
        public string? GrandFatherMotherName { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? SpouseName { get; set; }
        public string? Caste { get; set; }
        public string? Gender { get; set; }
        public string? Vdc { get; set; }
        public string? RegistrationOnBS { get; set; }
        public string? Status { get; set; }
        public string? MemberGroup { get; set; }
        public string? CitizenShipIssuedOnBs { get; set; }
        public string? CitizenShipIssuedDistrict { get; set; }
        public string? ElectricityNo { get; set; }
        public string? WaterSupplyNo { get; set; }
        public string? Education { get; set; }
        public string? IncomeRange { get; set; }
        public string? StateVDC { get; set; }
        public string? MemberYearlyPaymentTillDateOnBs { get; set; }
        public string? NameInNepali { get; set; }
        public string? AddressInNepali { get; set; }
        public string? MemberImage { get; set; }
    }
}
