using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class FamFixedAssetsClearanceStatus
{
    public int FamFixedAssetsClearanceStatusId { get; set; }

    public string? Status { get; set; }

    public string? Description { get; set; }
}
