using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class SycDepositType
{
    public long SycDepositTypeId { get; set; }

    public string DepositTypeCode { get; set; } = null!;

    public string DepositTypeName { get; set; } = null!;

    public int? Duration { get; set; }

    public string? DurationType { get; set; }

    /// <summary>
    /// D=Days, M=Month, Y=Year
    /// </summary>
    public long SycDepositCategoryId { get; set; }

    public decimal? InterestRate { get; set; }

    public long? SycInterestTypeId { get; set; }

    public long SycInterestCalculationPeriodId { get; set; }

    public decimal? TaxRate { get; set; }

    public decimal? MinimumDepositForInterest { get; set; }

    public decimal? MininumDepositAmount { get; set; }

    public decimal? MaximumDepositAmount { get; set; }

    public decimal? MinimumPerDayWithDrawal { get; set; }

    public decimal? MaxPerDayWithDrawal { get; set; }

    public decimal? MinimumBalance { get; set; }

    public bool? Widthdrawal { get; set; }

    public bool? Mandatory { get; set; }

    public decimal? CollectorCommission { get; set; }

    public long? CodeValue { get; set; }

    public string? InterestCalculationType { get; set; }

    public bool? IsCustomizable { get; set; }

    public bool IsActive { get; set; }

    public DateTime? InterestActivationOn { get; set; }

    public string? InterestActivationOnBs { get; set; }

    public long? SycInterestProvisioningInterestCalculationPeriodId { get; set; }

    public string? Remarks { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public DateOnly? LastInterestTransferDateOn { get; set; }

    public string? LastInterestTransferDateOnBs { get; set; }

    public string? DepositTypeInNepali { get; set; }

    public bool IsMobileApp { get; set; }
}
