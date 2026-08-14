using System;
using System.Collections.Generic;

namespace NexgenCosysReport.Models;

public partial class MamAccountOpening
{
    public long MamAccountOpeningId { get; set; }

    public long UsmOfficeId { get; set; }

    public long MemMemberRegistrationId { get; set; }

    public long? HurCollectorId { get; set; }

    public bool AccountNamingOption { get; set; }

    public bool MinorAccount { get; set; }

    public string? AccountName { get; set; }

    public string AccountNo { get; set; } = null!;

    public long SycDepositTypeId { get; set; }

    public long? MamAccountHolderTypeId { get; set; }

    public decimal? InterestRate { get; set; }

    public long? SycInterestCalculationTypeId { get; set; }

    public int? SycDepositMethodTypeId { get; set; }

    public long? MamInterestTransferAccountOpeningId { get; set; }

    public decimal? TaxRate { get; set; }

    public DateTime AccountOpenOn { get; set; }

    public string? AccountOpenOnBs { get; set; }

    public string? Certificates { get; set; }

    public string? Signature { get; set; }

    public decimal? LedgerBalance { get; set; }

    public decimal? FreezeAmount { get; set; }

    public DateTime? MaturityOn { get; set; }

    public string? MaturityOnBs { get; set; }

    public DateTime? NextInterestDateOn { get; set; }

    public string? NextInterestDateOnBs { get; set; }

    public bool Withdrawal { get; set; }

    public bool AccountClose { get; set; }

    public long? MamAccountStatusId { get; set; }

    public decimal? TermDepositInstallmentAmount { get; set; }

    public decimal? TermDepositMaturityAmount { get; set; }

    public decimal? TermDepositNoOfInstallment { get; set; }

    public string? TermDepositNoOfInstallmentType { get; set; }

    public long? SycSmsCategoryId { get; set; }

    public string? Remarks { get; set; }

    public decimal? BalanceCd { get; set; }

    public DateTime? LastInterestDateOn { get; set; }

    public string? LastInterestDateOnBs { get; set; }

    public bool IsInterestCalculationActive { get; set; }

    public bool IsInterestCreditable { get; set; }

    public bool IsInterestCustomized { get; set; }

    public decimal? InterestPayable { get; set; }

    public decimal? TaxReceivable { get; set; }

    public string? PendingTransactionRemarks { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? CreatedOnBs { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public string? LastModifiedOnBs { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsRenewed { get; set; }

    public decimal PreviousInterest { get; set; }

    public DateTime? PinCodeGeneratedOn { get; set; }

    public string? PinCode { get; set; }

    public DateTime? PinCodeLastChangedOn { get; set; }

    public decimal? MinimumAmount { get; set; }

    public decimal? MaximumAmount { get; set; }

    public bool IsFundTransferActive { get; set; }

    public string? ESewaId { get; set; }

    public long? PinCodeGeneratedBy { get; set; }

    public bool EnableMobileAppNotification { get; set; }

    public DateOnly? CompulsaryAmountLastChangedOn { get; set; }

    public string? CompulsaryAmountLastChangedOnBs { get; set; }

    public decimal? CompulsaryDueAmount { get; set; }

    public DateOnly? PenaltyTillDateOn { get; set; }

    public string? PenaltyTillDateOnBs { get; set; }

    public decimal? PreviousPenaltyAmount { get; set; }

    public int? CompulsaryDueAmountCount { get; set; }

    public decimal? CompulsaryDueAmountInstallment { get; set; }

    public bool? RenewAutomatically { get; set; }

    public DateTime? MinorBirthDateOn { get; set; }

    public string? MinorBirthDateOnBs { get; set; }

    public DateTime? MobileAppIssueDateOn { get; set; }

    public string? MobileAppIssueDateOnBs { get; set; }

    public DateTime? MobileAppMaturityDateOn { get; set; }

    public string? MobileAppMaturityDateOnBs { get; set; }

    public DateTime? MobileAppRenewDateOn { get; set; }

    public string? MobileAppRenewDateOnBs { get; set; }

    public bool IsRenewMobileApp { get; set; }

    public bool? IsVerify { get; set; }

    public long? VerifiedBy { get; set; }

    public DateTime? VerifiedOn { get; set; }

    public string? AccountNameInNepali { get; set; }

    public string? RegistrationNo { get; set; }

    public virtual HurCollector? HurCollector { get; set; }

    public virtual MamAccountHolderType? MamAccountHolderType { get; set; }

    public virtual MamAccountStatus? MamAccountStatus { get; set; }

    public virtual MemMemberRegistration MemMemberRegistration { get; set; } = null!;

    public virtual SycDepositType SycDepositType { get; set; } = null!;

    public virtual UsmOffice UsmOffice { get; set; } = null!;
}
