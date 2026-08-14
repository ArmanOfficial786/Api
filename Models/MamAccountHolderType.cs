using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class MamAccountHolderType
{
    public long MamAccountHolderTypeId { get; set; }

    public string AccountHolder { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<MamAccountOpening> MamAccountOpenings { get; set; } = new List<MamAccountOpening>();
}
