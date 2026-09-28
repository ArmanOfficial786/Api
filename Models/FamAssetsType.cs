using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class FamAssetsType
{
    public int FamAssetsTypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string AssetTypeCode { get; set; } = null!;

    public bool IsActive { get; set; }
}
