using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class FamAssetsCategory
{
    public int FamAssetsCategoryId { get; set; }

    public int FamAssetsTypeId { get; set; }

    public string CategoryName { get; set; } = null!;

    public bool IsActive { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    /// <summary>
    /// F= Fixed Assets, O=Other Assets
    /// </summary>
    public string? Type { get; set; }
}
