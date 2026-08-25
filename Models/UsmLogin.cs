using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmLogin
{
    public long UsmLoginId { get; set; }

    public long UsmUserId { get; set; }

    public DateTime LoginInOn { get; set; }

    public DateTime? LogOutOn { get; set; }

    public string? SessionId { get; set; }

    public virtual ICollection<UsmLoginActivity> UsmLoginActivities { get; set; } = new List<UsmLoginActivity>();

    public virtual UsmUser UsmUser { get; set; } = null!;
}
