using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class FamDepreciationMethod
{
    public int FamDepreciationMethodId { get; set; }

    public string MethodName { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<FamFixedAssetsDetail> FamFixedAssetsDetails { get; set; } = new List<FamFixedAssetsDetail>();
}
