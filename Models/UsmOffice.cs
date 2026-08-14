using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmOffice
{
    public long UsmOfficeId { get; set; }

    public int UsmOfficeTypeId { get; set; }

    public string OfficeName { get; set; } = null!;

    public string? OfficeNameNepali { get; set; }

    public string? OfficeDetailAddress { get; set; }

    public string? OfficeDetailAddressNepali { get; set; }

    public string? OfficeShortCode { get; set; }

    public string? PhoneNumbers { get; set; }

    public string? FaxNumbers { get; set; }

    public string? EmailAddress { get; set; }

    public string? OfficeUrl { get; set; }

    public int? SycVdcid { get; set; }

    public string? WardNo { get; set; }

    public string? Tole { get; set; }

    public DateTime? JoinedOn { get; set; }

    public string? JoinedOnBs { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public int? SycStateVdcid { get; set; }

    public virtual ICollection<HurCollector> HurCollectors { get; set; } = new List<HurCollector>();

    public virtual ICollection<LmtLoanTypeMaster> LmtLoanTypeMasters { get; set; } = new List<LmtLoanTypeMaster>();

    public virtual ICollection<MamAccountOpening> MamAccountOpenings { get; set; } = new List<MamAccountOpening>();

    public virtual ICollection<MemMemberRegistration> MemMemberRegistrations { get; set; } = new List<MemMemberRegistration>();

    public virtual ICollection<SycCollectionCenter> SycCollectionCenters { get; set; } = new List<SycCollectionCenter>();

    public virtual ICollection<SycMemberGroup> SycMemberGroups { get; set; } = new List<SycMemberGroup>();

    public virtual ICollection<UsmRelationUserToOffice> UsmRelationUserToOffices { get; set; } = new List<UsmRelationUserToOffice>();
}
