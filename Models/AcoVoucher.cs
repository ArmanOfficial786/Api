using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class AcoVoucher
{
    public long AcoVoucherId { get; set; }

    public long UsmOfficeId { get; set; }

    public string VoucherNo { get; set; } = null!;

    public DateTime VoucherOn { get; set; }

    public string? VoucherOnBs { get; set; }

    public string? FiscalYear { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }
}
