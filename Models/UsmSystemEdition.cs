using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmSystemEdition
{
    public int UsmSystemEditionId { get; set; }

    public string SystemEditionName { get; set; } = null!;

    public string? Descrption { get; set; }

    public bool IsActive { get; set; }

    public int? UsmTranslationId { get; set; }
}
