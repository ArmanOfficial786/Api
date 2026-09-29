namespace NexgenCosysReport.Models;

public partial class LmtPaymentDurationType
{
    public int LmtPaymentDurationTypeId { get; set; }

    public string PaymentDurationType { get; set; } = null!;

    public string? Description { get; set; }
    public virtual ICollection<LmtLoanIssue> LmtLoanIssues { get; set; } = new List<LmtLoanIssue>();
}
