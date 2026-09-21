namespace NexgenCosysReport.Dtos.RequestDtos.Share
{
    public class ShareDetailsRequestDto
    {
        public string? TillDateBs { get; set; }
        public string? OfficeIds { get; set; }
        public long ShareTypeId { get; set; } = -1;
        public long MemberTypeId { get; set; } = -1;
        public bool IsGreaterThan { get; set; } = true;
        public decimal TotalShareAmount { get; set; } = 0;
        public long CollectionCenterId { get; set; } = -1;
        public long MemberGroupId { get; set; } = -1;
        public string OrderBy { get; set; } = "MemberName";
        public bool EnableCollectionCenter { get; set; } = false;
        public bool EnableGroup { get; set; } = false;
        public string ReportType { get; set; } = "ENGLISH";
        public bool VisualReport { get; set; } = false;
    }

    public class ShareDetailsRowDto
    {
        public string? MemberId { get; set; }
        public string? Center { get; set; }
        public string? MemberGroup { get; set; }
        public string? GrandFatherName { get; set; }
        public string? FatherName { get; set; }
        public string? SpouceName { get; set; }
        public string? MemberName { get; set; }
        public string? MemberAge { get; set; }
        public string? MemberAddress { get; set; }
        public string? DateOfBirth { get; set; }
        public string? CitizenShipNo { get; set; }
        public string? Occupation { get; set; }
        public string? CitizenShipDistrict { get; set; }
        public string? CitizenShipDate { get; set; }
        public decimal? TotalShare { get; set; }
        public decimal? TotalShareAmount { get; set; }
        public string? RegistrationOn { get; set; }
        public string? RegistrationDate { get; set; }
        public string? NomineeName { get; set; }
        public string? NomineeAge { get; set; }
        public string? NomineeAddress { get; set; }
        public string? Signature { get; set; }
        public string? Remarks { get; set; }
        public string? Gender { get; set; }
        public string? MobileNo { get; set; }
        public string? ContactNo { get; set; }
    }

    public class ShareDetailsData
    {
        public List<ShareDetailsRowDto> Rows { get; set; } = [];
        public int TotalRecords { get; set; }
        public int TotalMembers { get; set; }
        public decimal TotalShare { get; set; }
        public decimal TotalShareAmount { get; set; }
        public string? TillDateBs { get; set; }
        public string? BranchName { get; set; }
        public string? ShareTypeName { get; set; }
        public string? MemberTypeName { get; set; }
        public string? CollectionCenterName { get; set; }
        public string? MemberGroupName { get; set; }
        public string? OrderBy { get; set; }
        public string? ReportType { get; set; }
    }
}
