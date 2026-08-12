using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class ShmShareType
{
    public int ShmShareTypeId { get; set; }

    public string ShareTypeName { get; set; } = null!;

    public decimal ShareFaceValue { get; set; }

    public decimal SharePremium { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? CreatedOnBs { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? LastModifiedOnBs { get; set; }

    public bool IsTransferable { get; set; }

    public bool IsActive { get; set; }

    public string? ShareTypeNameInNepali { get; set; }
}
