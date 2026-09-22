using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class SycMemberType
{
    public int SycMemberTypeId { get; set; }

    public string MemberTypeName { get; set; } = null!;

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? Description { get; set; }
}
