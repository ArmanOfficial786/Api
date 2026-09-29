using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class LmtLoanPaymentMethod
{
    public int LmtLoanPaymentMothodId { get; set; }

    public string? LoanPaymentMethod { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<LmtLoanIssue> LmtLoanIssues { get; set; } = new List<LmtLoanIssue>();
}
