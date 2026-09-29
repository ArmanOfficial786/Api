using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class SycSmsCategory
{
    public long SycSmsCategoryId { get; set; }

    public string SmsCategory { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<LmtLoanIssue> LmtLoanIssues { get; set; } = new List<LmtLoanIssue>();
}
