using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmRelationUserToOffice
{
    public long UsmUserId { get; set; }

    public long UsmOfficeId { get; set; }

    public virtual UsmOffice UsmOffice { get; set; } = null!;
}
