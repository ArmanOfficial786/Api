using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmLoginActivity
{
    public long UsmLoginActivityId { get; set; }

    public long UsmLoginId { get; set; }

    public int UsmMenuControlId { get; set; }

    public DateTime ActivityOn { get; set; }

    public string? ActivityOnBs { get; set; }

    public string? Description { get; set; }

    public virtual UsmLogin UsmLogin { get; set; } = null!;
}
