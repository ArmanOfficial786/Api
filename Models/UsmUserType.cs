using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmUserType
{
    public long UsmUserTypeId { get; set; }

    public string UserTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public virtual ICollection<UsmUser> UsmUsers { get; set; } = new List<UsmUser>();
}
