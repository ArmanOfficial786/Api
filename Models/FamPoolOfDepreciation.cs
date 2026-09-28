using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class FamPoolOfDepreciation
{
    public int FamPoolOfDepreciationId { get; set; }

    public string? Name { get; set; }

    public decimal? Percentage { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<FamFixedAssetsDetail> FamFixedAssetsDetails { get; set; } = new List<FamFixedAssetsDetail>();
}
