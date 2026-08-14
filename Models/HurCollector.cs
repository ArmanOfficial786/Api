using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class HurCollector
{
    public long HurCollectorId { get; set; }

    public int SycSalutationId { get; set; }

    public string CollectorFullName { get; set; } = null!;

    public string? CollectorCode { get; set; }

    public int SycGenderId { get; set; }

    public long UsmOfficeId { get; set; }

    public DateTime? JoinedOn { get; set; }

    public string? JoinedOnBs { get; set; }

    public string? CollectorDetailAddress { get; set; }

    public string? MobileNumber { get; set; }

    public string? PhoneNumber { get; set; }

    public string? EmailAddress { get; set; }

    public bool Commission { get; set; }

    public string? PhotoFile { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? Remarks { get; set; }

    public bool EnableMobileApp { get; set; }

    public string? Imeino { get; set; }

    public string? DeviceId { get; set; }

    public string? PinCode { get; set; }

    public virtual ICollection<MamAccountOpening> MamAccountOpenings { get; set; } = new List<MamAccountOpening>();

    public virtual ICollection<SycCollectionCenter> SycCollectionCenters { get; set; } = new List<SycCollectionCenter>();

    public virtual UsmOffice UsmOffice { get; set; } = null!;
}
