using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class LmtLoanStatus
{
    public int LmtLoanStatusId { get; set; }

    public string LoanStatus { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<LmtLoanIssue> LmtLoanIssues { get; set; } = new List<LmtLoanIssue>();
}
