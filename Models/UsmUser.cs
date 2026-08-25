using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class UsmUser
{
    public long UsmUserId { get; set; }

    public long UsmOfficeId { get; set; }

    public int UsmGenderId { get; set; }

    public long UsmUserTypeId { get; set; }

    public string FullName { get; set; } = null!;

    public string UserCode { get; set; } = null!;

    public string LoginEmailAddress { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? UserDetailAddress { get; set; }

    public string? UserPhoneNumber { get; set; }

    public int? PasswordExpDays { get; set; }

    public bool IsActive { get; set; }

    public string? Remarks { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public DateTime? PasswordChangedOn { get; set; }

    public bool EnableTellerLimit { get; set; }

    public decimal TellerLimit { get; set; }

    public bool EnableWithdrawlLimit { get; set; }

    public decimal WithdrawlLimit { get; set; }

    public string? FullNameInNepali { get; set; }

    public virtual ICollection<UsmLogin> UsmLogins { get; set; } = new List<UsmLogin>();

    public virtual UsmOffice UsmOffice { get; set; } = null!;

    public virtual UsmUserType UsmUserType { get; set; } = null!;

    public virtual ICollection<UsmRelationUserToOffice> UsmRelationUserToOffices { get; set; } = new List<UsmRelationUserToOffice>();

    public virtual ICollection<UsmRelationUserToOfficeLogin> UsmRelationUserToOfficeLogins { get; set; } = new List<UsmRelationUserToOfficeLogin>();
}
