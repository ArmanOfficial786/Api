using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class AcoFiscalYear
{
    public int AcoFiscalYearId { get; set; }

    public string FiscalYear { get; set; } = null!;

    public DateTime? FiscalYearFromOn { get; set; }

    public string? FiscalYearFromOnBs { get; set; }

    public DateTime? FiscalYearToOn { get; set; }

    public string? FiscalYearToOnBs { get; set; }

    public bool IsFiscalYearClosed { get; set; }
}
