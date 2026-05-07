using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class SycMemberGroup
{
    public int SycMemberGroupId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime? RegistrationOn { get; set; }

    public string? RegistrationOnBs { get; set; }

    public long? UsmOfficeId { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? Description { get; set; }

    public long? SycCollectionCenterId { get; set; }

    public string? GroupShortCode { get; set; }

    public string? Address { get; set; }

    public string? ContactNo { get; set; }

    public virtual ICollection<MemMemberRegistration> MemMemberRegistrations { get; set; } = new List<MemMemberRegistration>();

    public virtual SycCollectionCenter? SycCollectionCenter { get; set; }

    public virtual UsmOffice? UsmOffice { get; set; }
}
