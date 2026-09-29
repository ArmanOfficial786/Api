using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class LmtLoanPaymentType
{
    public int LmtLoanPaymentTypeId { get; set; }

    public string? LoanPaymentTypeCode { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<LmtLoanIssue> LmtLoanIssues { get; set; } = new List<LmtLoanIssue>();
}
