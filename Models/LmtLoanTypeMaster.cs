using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class LmtLoanTypeMaster
{
    public long LmtLoanTypeMasterId { get; set; }

    public string LoanTypeCode { get; set; } = null!;

    public string LoanTypeName { get; set; } = null!;

    public int LmtLoanCategoryId { get; set; }

    public long LmtLoanTypeId { get; set; }

    public int Period { get; set; }

    public string PeriodType { get; set; } = null!;

    public string? InterestRate { get; set; }

    public decimal? MaximumAmount { get; set; }

    public long CodeValue { get; set; }

    public string? LoanOdorNormal { get; set; }

    public long? UsmOfficeId { get; set; }

    public bool IsActive { get; set; }

    public string? Remarks { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public DateTime? InterestActivationOn { get; set; }

    public string? InterestActivationOnBs { get; set; }

    /// <summary>
    /// N=FlatInterest ,D= Claculate Interest by Ason Interest
    /// </summary>
    public string? InterestCalculationType { get; set; }

    public decimal? Commission { get; set; }

    public string? LoanTypeNameInNepali { get; set; }

    public virtual UsmOffice? UsmOffice { get; set; }
}
