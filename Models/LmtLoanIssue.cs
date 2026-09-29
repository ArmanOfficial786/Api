namespace NexgenCosysReport.Models;

public partial class LmtLoanIssue
{
    public long LmtLoanIssueId { get; set; }

    public long MemMemberRegistrationId { get; set; }

    public string LoanAccountNo { get; set; } = null!;

    public long LmtLoanTypeMasterId { get; set; }

    public decimal? LoanSanctionAmount { get; set; }

    public decimal? LoanIssueAmount { get; set; }

    public decimal Period { get; set; }

    public string PeriodType { get; set; } = null!;

    public int? LmtPaymentDurationTypeId { get; set; }

    public decimal? InterestRate { get; set; }

    public int LmtLoanPaymentTypeId { get; set; }

    public DateOnly LoanIssueOn { get; set; }

    public string? LoanIssueOnBs { get; set; }

    public decimal? PrinciplePaidFrom { get; set; }

    public decimal? PrinciplePaidAfterEach { get; set; }

    public DateTime MaturityOn { get; set; }

    public string? MaturityOnBs { get; set; }

    /// <summary>
    /// C - For Cash and B - for Bank
    /// </summary>
    public int? LmtLoanPaymentMethodId { get; set; }

    public decimal? NetPaidAmount { get; set; }

    public int? LmtLoanStatusId { get; set; }

    public long? MamAccountOpeningId { get; set; }

    /// <summary>
    /// O=OverDraft Loan, N=Normal Loan
    /// </summary>
    public string LoanOdorNormal { get; set; } = null!;

    public decimal? RevolvingSanctionAmount { get; set; }

    public long? RevolvingSavingAcid { get; set; }

    public decimal? RevolvingBalanceAmount { get; set; }

    public long? HurCollectorId { get; set; }

    public long? UsmOfficeId { get; set; }

    /// <summary>
    /// I=Issue, U=Loan Overdraft Update, R=Loan Issue Renewed
    /// </summary>
    public string? TransStatus { get; set; }

    public bool? IsVerified { get; set; }

    public string? VerifiedBy { get; set; }

    public DateTime? VerifiedOn { get; set; }

    public long CreatedBy { get; set; }

    public DateTime CreatedOn { get; set; }

    public long? LastModifiedBy { get; set; }

    public DateTime? LastModifiedOn { get; set; }

    public bool IsActive { get; set; }

    public string? Remarks { get; set; }

    public string? LoanGuarantee { get; set; }

    public bool IsEdited { get; set; }

    public bool IsPosted { get; set; }

    public decimal? InterestReceivableAmount { get; set; }

    public string? InterestReceivableTillDateOnBs { get; set; }

    public DateTime? InterestReceivableTillDateOn { get; set; }

    public decimal? PenaltyReceivableAmount { get; set; }

    public string? PenaltyReceivableTillDateOnBs { get; set; }

    public DateTime? PenaltyReceivableTillDateOn { get; set; }

    public decimal? InstallamentAmount { get; set; }

    /// <summary>
    /// AOD (As On Date),ME (Monthly End)
    /// </summary>
    public string? InstallmentType { get; set; }

    /// <summary>
    /// 1= Interest, 2= Principle, 3= Principle and Interest, 4= None
    /// </summary>
    public int? PenaltyPaymentMethod { get; set; }

    /// <summary>
    /// 1= As on Date, 2= Schedule Wise
    /// </summary>
    public int? LoanPaymentMethod { get; set; }

    public DateTime? LoanCloseOn { get; set; }

    public string? LoanCloseOnBs { get; set; }

    public long? SycSmsCategoryId { get; set; }

    public int LoanScheduleInterval { get; set; }

    public bool? IsOverDraftPayment { get; set; }

    public bool IsProcessed { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public string? ProcessedOnBs { get; set; }

    public int? ProcessedBy { get; set; }

    public int? SycMemberGroupId { get; set; }

    public long? SycCollectionCenterId { get; set; }

    public DateOnly? FirstInstallmentOn { get; set; }

    public string? FirstInstallmentOnBs { get; set; }

    public bool EnableMobileAppNotification { get; set; }

    public string? LoanScheduleDateType { get; set; }

    public DateOnly? LoanRescheduleDateOn { get; set; }

    public string? LoanRescheduleDateOnBs { get; set; }

    public string? UploadedDocument { get; set; }

    public virtual LmtLoanPaymentMethod? LmtLoanPaymentMethod { get; set; }

    public virtual LmtLoanPaymentType LmtLoanPaymentType { get; set; } = null!;

    public virtual LmtLoanStatus? LmtLoanStatus { get; set; }

    public virtual SycSmsCategory? SycSmsCategory { get; set; }


    // ---- Navigations ----
    public virtual HurCollector? HurCollector { get; set; }
    public virtual LmtLoanTypeMaster? LmtLoanTypeMaster { get; set; }
    public virtual LmtPaymentDurationType? LmtPaymentDurationType { get; set; }
    public virtual MamAccountOpening? MamAccountOpening { get; set; }
    public virtual MemMemberRegistration? MemMemberRegistration { get; set; }
    public virtual SycCollectionCenter? SycCollectionCenter { get; set; }
    public virtual SycMemberGroup? SycMemberGroup { get; set; }
    public virtual UsmOffice? UsmOffice { get; set; }
}
