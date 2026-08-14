using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class MamAccountStatus
{
    public long MamAccountStatusId { get; set; }

    public string AccountStatus { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<MamAccountOpening> MamAccountOpenings { get; set; } = new List<MamAccountOpening>();
}
