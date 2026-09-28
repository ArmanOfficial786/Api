using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class RemRemittanceType
{
    public long RemRemittanceTypeId { get; set; }

    public string RemittanceTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedOnBs { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? LastModifiedOnBs { get; set; }
}
