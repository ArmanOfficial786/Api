using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class SycCompany
{
    public int SycCompanyId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? CompanyNameNepali { get; set; }

    public string CompanyAddress { get; set; } = null!;

    public string? CompanyAddressNepali { get; set; }

    public string? PhoneNo { get; set; }

    public string? FaxNo { get; set; }

    public string? CompanyEmailId { get; set; }

    public string? CompanyWebSite { get; set; }

    public string? CompanyLogo { get; set; }

    public string? LicenseKey { get; set; }

    public string? CurrentVersion { get; set; }

    public string? Description { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public bool LicenceIsActive { get; set; }

    public string? ClientCode { get; set; }

    public bool EnableGoogleDriveBackUp { get; set; }

    public string? PanNo { get; set; }

    public string? RegisteredNo { get; set; }

    public string ServerKey { get; set; } = null!;

    public string BroadCastId { get; set; } = null!;

    public string? AppType { get; set; }

    public string? NoReplyEmail { get; set; }
}
