using System;
using System.Collections.Generic;

namespace JsSampleReport.Models;

public partial class SycCollectionCenter
{
    public long SycCollectionCenterId { get; set; }

    public string CollectionCenterName { get; set; } = null!;

    public string CollectionCenterShortCode { get; set; } = null!;

    public string? Address { get; set; }

    public string? ContactNo { get; set; }

    public DateTime RegistrationOn { get; set; }

    public string RegistrationOnBs { get; set; } = null!;

    public long UsmOfficeId { get; set; }

    public int? SycVdcid { get; set; }

    public DateTime? MeetingStartDateOn { get; set; }

    public string? MeetingStartDateOnBs { get; set; }

    public int? Duration { get; set; }

    /// <summary>
    /// D=Days, W=Weeks, M=Months,  Y=Years
    /// </summary>
    public string? DurationType { get; set; }

    public bool IsActive { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? Description { get; set; }

    public bool? CreateScheduleByCenterMeetingDate { get; set; }

    public string? InstallmentType { get; set; }

    public string? LoanScheduleDateType { get; set; }

    public long? HurCollectorId { get; set; }

    public string? MeetingTime { get; set; }

    public bool? EnableMemberYearlyPayment { get; set; }

    public decimal? MemberYearlyPaymentAmount { get; set; }

    public long? MemberYearlyPaymentLedgerId { get; set; }

    public virtual ICollection<SycMemberGroup> SycMemberGroups { get; set; } = new List<SycMemberGroup>();

    public virtual UsmOffice UsmOffice { get; set; } = null!;
}
