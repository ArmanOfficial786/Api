using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace NexgenCosysReport.Models;

[PrimaryKey(nameof(UsmUserId), nameof(UsmOfficeId))]
public partial class UsmRelationUserToOfficeLogin
{
    public long UsmUserId { get; set; }

    public long UsmOfficeId { get; set; }

    public virtual UsmUser UsmUser { get; set; } = null!;
    public virtual UsmOffice UsmOffice { get; set; } = null!;
}
