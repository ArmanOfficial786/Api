namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class CopomisRequestDto
    {
        public string? TillDateBs { get; set; }
        public string? OfficeIds { get; set; }
        public long MemberTypeId { get; set; } = -1;
        public long CollectionCenterId { get; set; } = -1;
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberName";
        public bool ShowMemberPhoto { get; set; } = true;
        public bool VisualReport { get; set; } = false;
    }

    public class CopomisRowDto
    {
        public long? MemMemberRegistrationId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }
        public string? Gender { get; set; }
        public int? Age { get; set; }
        public string? BirthOnBS { get; set; }
        public string? BirthOn { get; set; }
        public string? GrandFatherMotherName { get; set; }
        public string? GrandMotherName { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? CitizenshipNo { get; set; }
        public string? CitizenShipIssuedDistrict { get; set; }
        public string? CitizenShipIssuedOn { get; set; }
        public string? CitizenShipIssuedOnBs { get; set; }
        public string? VotersId { get; set; }
        public string? PanVatNo { get; set; }
        public string? DrivingLicenseNo { get; set; }
        public string? PassportNo { get; set; }
        public string? DistrictName { get; set; }
        public string? VDCName { get; set; }
        public string? Tole { get; set; }
        public string? WardNo { get; set; }
        public string? HouseNo { get; set; }
        public string? PhoneNo { get; set; }
        public string? MobileNo { get; set; }
        public string? Occupation { get; set; }
        public string? Religion { get; set; }
        public string? Caste { get; set; }
        public string? MaritualStatus { get; set; }
        public string? EmailAddress { get; set; }
        public string? ElectricityNo { get; set; }
        public string? WaterSupplyNo { get; set; }
        public string? GPSCoOrdinate { get; set; }
        public string? SpouseName { get; set; }
        public string? NomineeName { get; set; }
        public string? NomineeRelation { get; set; }
        public string? NomineeContactNo { get; set; }
        public string? Son1 { get; set; }
        public string? Son2 { get; set; }
        public string? Son3 { get; set; }
        public string? Daughter1 { get; set; }
        public string? Daughter2 { get; set; }
        public string? Daughter3 { get; set; }
        public string? FatherInLawName { get; set; }
        public string? MotherInLawName { get; set; }
        public string? RegistrationOn { get; set; }
        public string? RegistrationOnBS { get; set; }
        public decimal? ShareNo { get; set; }
        public decimal? TotalShareAmt { get; set; }
        public string? MemberImage { get; set; }
    }

    public class CopomisData
    {
        public List<CopomisRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalShareNo { get; set; }
        public decimal TotalShareAmount { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? MemberTypeName { get; set; }
        public string? OrderBy { get; set; }
    }
}
