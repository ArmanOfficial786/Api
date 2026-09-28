using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class FamFixedAssetsDetail
{
    public long FamFixedAssetsDetailId { get; set; }

    public int FamAssetsCategoryId { get; set; }

    public int FamDepreciationMethodId { get; set; }

    public int FamPoolOfDepreciationId { get; set; }

    public string FixedAssetsName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual FamDepreciationMethod FamDepreciationMethod { get; set; } = null!;

    public virtual FamPoolOfDepreciation FamPoolOfDepreciation { get; set; } = null!;
}
