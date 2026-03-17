using System;
using System.Collections.Generic;

namespace JsSampleReport.Models;

public partial class MemMemberRegistration
{
    public long MemMemberRegistrationId { get; set; }

    public string MemberId { get; set; } = null!;

    public int SycMemberTypeId { get; set; }

    public long UsmOfficeId { get; set; }

    public int? SycSalutationId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? BirthOn { get; set; }

    public string? BirthOnBs { get; set; }

    public string? PermanentAddessDetail { get; set; }

    public string? TemporaryAddressDetail { get; set; }

    public string? PhoneNo { get; set; }

    public string? MobileNo { get; set; }

    public string? EmailAddress { get; set; }

    public int? SycNationalityId { get; set; }

    public string? CitizenshipNo { get; set; }

    public string? PassportNo { get; set; }

    public int? SycOccupationId { get; set; }

    public string? OtherOccupation { get; set; }

    public int? SycReligionId { get; set; }

    public int? SycMaritalStatusId { get; set; }

    public string? GrandFatherMotherName { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public string? SpouseName { get; set; }

    public int? SycCasteId { get; set; }

    public int? SycGenderId { get; set; }

    public int? SycVdcId { get; set; }

    public DateOnly RegistrationOn { get; set; }

    public string? RegistrationOnBs { get; set; }

    public long? IntroducedBy { get; set; }

    public int? SycStatusId { get; set; }

    public int SycMemberGroupId { get; set; }

    public string? Remarks { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedOnBs { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? LastModifiedOnBs { get; set; }

    public DateTime? CitizenShipIssuedOn { get; set; }

    public string? CitizenShipIssuedOnBs { get; set; }

    public string? CitizenShipIssuedDistrict { get; set; }

    public string? ElectricityNo { get; set; }

    public string? WaterSupplyNo { get; set; }

    public string? Imeino { get; set; }

    public int? SycEducationId { get; set; }

    public int? SycIncomeRangeId { get; set; }

    public string? DeviceId { get; set; }

    public string? GpscoOrdinate { get; set; }

    public int? SycStateVdcid { get; set; }

    public DateOnly? MemberYearlyPaymentTillDateOn { get; set; }

    public string? MemberYearlyPaymentTillDateOnBs { get; set; }

    public string? NameInNepali { get; set; }

    public string? AddressInNepali { get; set; }

    public string? DrivingLicenseNo { get; set; }

    public string? DrivingIssuePlace { get; set; }

    public DateOnly? DrivingValidOn { get; set; }

    public string? DrivingValidOnBs { get; set; }

    public string? VotersId { get; set; }

    public string? PanVatNo { get; set; }

    public string? LandLordName { get; set; }

    public string? LandLordContact { get; set; }

    public string? WardNo { get; set; }

    public string? HouseNo { get; set; }

    public string? Street { get; set; }

    public string? Tole { get; set; }

    public string? NationalId { get; set; }

    public string? NationalIdIssuingAuthority { get; set; }

    public string? GrandMotherName { get; set; }

    public string? DaughterName { get; set; }

    public string? SonName { get; set; }

    public string? FatherInLawName { get; set; }

    public string? MotherInLawName { get; set; }

    public bool IsPermanent { get; set; }

    public string? DaughterName2 { get; set; }

    public string? DaughterName3 { get; set; }

    public string? Son2 { get; set; }

    public string? Son3 { get; set; }

    public string? FatherNameInNepali { get; set; }

    public string? MotherNameInNepali { get; set; }

    public string? SpouseNameInNepali { get; set; }

    public string? DistrictNameInNepali { get; set; }

    public string? VdcmunicipalityNameInNepali { get; set; }

    public string? ToleNameInNepali { get; set; }

    public bool? IsVerify { get; set; }

    public long? VerifiedBy { get; set; }

    public DateTime? VerifiedOn { get; set; }

    public string? RegistrationNo { get; set; }
}
