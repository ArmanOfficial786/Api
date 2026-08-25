using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Models;

namespace NexgenCosysReport.DbContext;

public partial class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcoFiscalYear> AcoFiscalYears { get; set; }

    public virtual DbSet<ComCalendar> ComCalendars { get; set; }

    public virtual DbSet<HurCollector> HurCollectors { get; set; }

    public virtual DbSet<LmtLoanTypeMaster> LmtLoanTypeMasters { get; set; }

    public virtual DbSet<MamAccountHolderType> MamAccountHolderTypes { get; set; }

    public virtual DbSet<MamAccountOpening> MamAccountOpenings { get; set; }

    public virtual DbSet<MamAccountStatus> MamAccountStatuses { get; set; }

    public virtual DbSet<MemMemberRegistration> MemMemberRegistrations { get; set; }

    public virtual DbSet<ShmShareType> ShmShareTypes { get; set; }

    public virtual DbSet<SycCollectionCenter> SycCollectionCenters { get; set; }

    public virtual DbSet<SycCompany> SycCompanies { get; set; }

    public virtual DbSet<SycDepositType> SycDepositTypes { get; set; }

    public virtual DbSet<SycMemberGroup> SycMemberGroups { get; set; }

    public virtual DbSet<UsmLogin> UsmLogins { get; set; }

    public virtual DbSet<UsmLoginActivity> UsmLoginActivities { get; set; }

    public virtual DbSet<UsmOffice> UsmOffices { get; set; }

    public virtual DbSet<UsmSystemEdition> UsmSystemEditions { get; set; }

    public virtual DbSet<UsmUser> UsmUsers { get; set; }

    public virtual DbSet<UsmUserType> UsmUserTypes { get; set; }

    public virtual DbSet<UsmRelationUserToOffice> UsmRelationUserToOffices { get; set; }

    public virtual DbSet<UsmRelationUserToOfficeLogin> UsmRelationUserToOfficeLogins { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AcoFiscalYear>(entity =>
        {
            entity.ToTable("AcoFiscalYear");

            entity.Property(e => e.AcoFiscalYearId).ValueGeneratedNever();
            entity.Property(e => e.FiscalYear).HasMaxLength(10);
            entity.Property(e => e.FiscalYearFromOn).HasColumnType("datetime");
            entity.Property(e => e.FiscalYearFromOnBs).HasMaxLength(50);
            entity.Property(e => e.FiscalYearToOn).HasColumnType("datetime");
            entity.Property(e => e.FiscalYearToOnBs).HasMaxLength(50);
        });

        modelBuilder.Entity<ComCalendar>(entity =>
        {
            entity.ToTable("ComCalendar");

            entity.Property(e => e.ComCalendarId).ValueGeneratedNever();
            entity.Property(e => e.EnglishEndDate).HasColumnType("datetime");
            entity.Property(e => e.EnglishStartDate).HasColumnType("datetime");
        });

        modelBuilder.Entity<HurCollector>(entity =>
        {
            entity.ToTable("HurCollector");

            entity.Property(e => e.CollectorCode).HasMaxLength(10);
            entity.Property(e => e.CollectorDetailAddress).HasMaxLength(200);
            entity.Property(e => e.CollectorFullName).HasMaxLength(100);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.EmailAddress).HasMaxLength(100);
            entity.Property(e => e.Imeino).HasColumnName("IMEINo");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_SycCollector_IsActive");
            entity.Property(e => e.JoinedOn).HasColumnType("datetime");
            entity.Property(e => e.JoinedOnBs)
                .HasMaxLength(50)
                .HasColumnName("JoinedOnBS");
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.MobileNumber).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(100);
            entity.Property(e => e.PhotoFile).HasMaxLength(500);
            entity.Property(e => e.PinCode).HasMaxLength(10);
            entity.Property(e => e.Remarks).HasColumnType("ntext");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.HurCollectors)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HurCollector_UsmOffice");
        });

        modelBuilder.Entity<LmtLoanTypeMaster>(entity =>
        {
            entity.ToTable("LmtLoanTypeMaster");

            entity.Property(e => e.Commission).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.InterestActivationOn).HasColumnType("datetime");
            entity.Property(e => e.InterestActivationOnBs)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.InterestCalculationType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasComment("N=FlatInterest ,D= Claculate Interest by Ason Interest");
            entity.Property(e => e.InterestRate)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_LmtLoanType_IsActive");
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LoanOdorNormal)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("LoanODorNormal");
            entity.Property(e => e.LoanTypeCode).HasMaxLength(20);
            entity.Property(e => e.LoanTypeName).HasMaxLength(200);
            entity.Property(e => e.MaximumAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.PeriodType).HasMaxLength(1);
            entity.Property(e => e.Remarks).HasColumnType("ntext");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.LmtLoanTypeMasters)
                .HasForeignKey(d => d.UsmOfficeId)
                .HasConstraintName("FK_LmtLoanTypeMaster_UsmOffice");
        });

        modelBuilder.Entity<MamAccountHolderType>(entity =>
        {
            entity.ToTable("MamAccountHolderType");

            entity.Property(e => e.MamAccountHolderTypeId).ValueGeneratedNever();
            entity.Property(e => e.AccountHolder).HasMaxLength(50);
            entity.Property(e => e.Description).HasColumnType("ntext");
        });

        modelBuilder.Entity<MamAccountOpening>(entity =>
        {
            entity.ToTable("MamAccountOpening");

            entity.HasIndex(e => e.MemMemberRegistrationId, "NonClusteredIndex-MemMemberRegistrationId");

            entity.HasIndex(e => e.AccountNo, "NonClusteredIndex_AccountNo_INCLUDE_MamAccountOpeningId");

            entity.Property(e => e.AccountClose).HasDefaultValue(true, "DF_MamAccountOpening_AccountClose");
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.AccountNamingOption).HasComment("");
            entity.Property(e => e.AccountNo).HasMaxLength(50);
            entity.Property(e => e.AccountOpenOn).HasColumnType("datetime");
            entity.Property(e => e.AccountOpenOnBs).HasMaxLength(50);
            entity.Property(e => e.BalanceCd)
                .HasDefaultValue(0m, "DF_MamAccountOpening_BalanceCd")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Certificates).HasMaxLength(255);
            entity.Property(e => e.CompulsaryAmountLastChangedOnBs)
                .HasMaxLength(10)
                .HasColumnName("CompulsaryAmountLastChangedOnBS");
            entity.Property(e => e.CompulsaryDueAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.CompulsaryDueAmountInstallment).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedOnBs).HasMaxLength(50);
            entity.Property(e => e.ESewaId)
                .HasMaxLength(50)
                .HasColumnName("eSewaId");
            entity.Property(e => e.FreezeAmount)
                .HasDefaultValue(0m, "DF_MamAccountOpening_FreezeAmount")
                .HasColumnType("numeric(18, 2)");
            entity.Property(e => e.InterestPayable)
                .HasDefaultValue(0m, "DF_MamAccountOpening_InterestPayable")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InterestRate).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.IsInterestCalculationActive).HasDefaultValue(true, "DF_MamAccountOpening_IsInterestCalculationActive");
            entity.Property(e => e.IsInterestCreditable).HasDefaultValue(true, "DF_MamAccountOpening_IsInterestCreditable");
            entity.Property(e => e.IsVerify)
                .IsRequired()
                .HasDefaultValueSql("('1')");
            entity.Property(e => e.LastInterestDateOn).HasColumnType("datetime");
            entity.Property(e => e.LastInterestDateOnBs).HasMaxLength(50);
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LastModifiedOnBs).HasMaxLength(50);
            entity.Property(e => e.LedgerBalance)
                .HasDefaultValue(0m, "DF_MamAccountOpening_LedgerBalance")
                .HasColumnType("numeric(18, 2)");
            entity.Property(e => e.MaturityOn).HasColumnType("datetime");
            entity.Property(e => e.MaturityOnBs).HasMaxLength(50);
            entity.Property(e => e.MaximumAmount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.MinimumAmount).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.MinorAccount).HasComment("");
            entity.Property(e => e.MinorBirthDateOn).HasColumnType("datetime");
            entity.Property(e => e.MinorBirthDateOnBs).HasMaxLength(10);
            entity.Property(e => e.MobileAppIssueDateOn).HasColumnType("datetime");
            entity.Property(e => e.MobileAppIssueDateOnBs).HasMaxLength(50);
            entity.Property(e => e.MobileAppMaturityDateOn).HasColumnType("datetime");
            entity.Property(e => e.MobileAppMaturityDateOnBs).HasMaxLength(50);
            entity.Property(e => e.MobileAppRenewDateOn).HasColumnType("datetime");
            entity.Property(e => e.MobileAppRenewDateOnBs).HasMaxLength(50);
            entity.Property(e => e.NextInterestDateOn).HasColumnType("datetime");
            entity.Property(e => e.NextInterestDateOnBs).HasMaxLength(50);
            entity.Property(e => e.PenaltyTillDateOnBs).HasMaxLength(10);
            entity.Property(e => e.PinCode).HasMaxLength(10);
            entity.Property(e => e.PinCodeGeneratedOn).HasColumnType("datetime");
            entity.Property(e => e.PinCodeLastChangedOn).HasColumnType("datetime");
            entity.Property(e => e.PreviousInterest).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.PreviousPenaltyAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Remarks).HasColumnType("ntext");
            entity.Property(e => e.Signature).HasMaxLength(255);
            entity.Property(e => e.TaxRate).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.TaxReceivable)
                .HasDefaultValue(0m, "DF_MamAccountOpening_TaxReceivable")
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TermDepositInstallmentAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.TermDepositMaturityAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.TermDepositNoOfInstallment).HasColumnType("numeric(18, 0)");
            entity.Property(e => e.TermDepositNoOfInstallmentType).HasMaxLength(1);
            entity.Property(e => e.VerifiedOn).HasColumnType("datetime");
            entity.Property(e => e.Withdrawal).HasDefaultValue(true, "DF_MamAccountOpening_Withdrawal");

            entity.HasOne(d => d.HurCollector).WithMany(p => p.MamAccountOpenings)
                .HasForeignKey(d => d.HurCollectorId)
                .HasConstraintName("FK_MamAccountOpening_HurCollector");

            entity.HasOne(d => d.MamAccountHolderType).WithMany(p => p.MamAccountOpenings)
                .HasForeignKey(d => d.MamAccountHolderTypeId)
                .HasConstraintName("FK_MamAccountOpening_MamAccountHolderType");

            entity.HasOne(d => d.MamAccountStatus).WithMany(p => p.MamAccountOpenings)
                .HasForeignKey(d => d.MamAccountStatusId)
                .HasConstraintName("FK_MamAccountOpening_MamAccountStatus");

            entity.HasOne(d => d.MemMemberRegistration).WithMany(p => p.MamAccountOpenings)
                .HasForeignKey(d => d.MemMemberRegistrationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MamAccountOpening_MemMemberRegistration");

            entity.HasOne(d => d.SycDepositType).WithMany(p => p.MamAccountOpenings)
                .HasForeignKey(d => d.SycDepositTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MamAccountOpening_SycDepositType");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.MamAccountOpenings)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MamAccountOpening_UsmOffice");
        });

        modelBuilder.Entity<MamAccountStatus>(entity =>
        {
            entity.ToTable("MamAccountStatus");

            entity.Property(e => e.MamAccountStatusId).ValueGeneratedNever();
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Description).HasColumnType("ntext");
        });

        modelBuilder.Entity<MemMemberRegistration>(entity =>
        {
            entity.ToTable("MemMemberRegistration");

            entity.HasIndex(e => e.MemberId, "IX_MemMemberRegistration").IsUnique();

            entity.Property(e => e.BirthOnBs)
                .HasMaxLength(20)
                .HasColumnName("BirthOnBS");
            entity.Property(e => e.BloodGroup).HasMaxLength(10);
            entity.Property(e => e.CitizenShipIssuedDistrict).HasMaxLength(50);
            entity.Property(e => e.CitizenShipIssuedOn).HasColumnType("datetime");
            entity.Property(e => e.CitizenShipIssuedOnBs).HasMaxLength(50);
            entity.Property(e => e.CitizenshipNo).HasMaxLength(50);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedOnBs).HasMaxLength(50);
            entity.Property(e => e.DaughterName).HasMaxLength(50);
            entity.Property(e => e.DaughterName2).HasMaxLength(225);
            entity.Property(e => e.DaughterName3).HasMaxLength(225);
            entity.Property(e => e.DeviceId).HasMaxLength(255);
            entity.Property(e => e.DrivingIssuePlace).HasMaxLength(50);
            entity.Property(e => e.DrivingLicenseNo).HasMaxLength(50);
            entity.Property(e => e.DrivingValidOnBs).HasMaxLength(50);
            entity.Property(e => e.ElectricityNo).HasMaxLength(50);
            entity.Property(e => e.EmailAddress).HasMaxLength(100);
            entity.Property(e => e.FatherInLawName).HasMaxLength(50);
            entity.Property(e => e.FatherName).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.GpscoOrdinate)
                .HasMaxLength(50)
                .HasColumnName("GPSCoOrdinate");
            entity.Property(e => e.GrandFatherMotherName).HasMaxLength(100);
            entity.Property(e => e.GrandMotherName).HasMaxLength(50);
            entity.Property(e => e.HouseNo).HasMaxLength(50);
            entity.Property(e => e.Imeino)
                .HasMaxLength(50)
                .HasColumnName("IMEINo");
            entity.Property(e => e.IsVerify)
                .IsRequired()
                .HasDefaultValueSql("('1')");
            entity.Property(e => e.LandLordContact).HasMaxLength(50);
            entity.Property(e => e.LandLordName).HasMaxLength(50);
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LastModifiedOnBs).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.MemberId).HasMaxLength(50);
            entity.Property(e => e.MemberYearlyPaymentTillDateOnBs).HasMaxLength(10);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.MobileNo).HasMaxLength(50);
            entity.Property(e => e.MotherInLawName).HasMaxLength(50);
            entity.Property(e => e.MotherName).HasMaxLength(100);
            entity.Property(e => e.NationalId).HasMaxLength(50);
            entity.Property(e => e.NationalIdIssuingAuthority).HasMaxLength(50);
            entity.Property(e => e.OtherOccupation).HasMaxLength(100);
            entity.Property(e => e.PanVatNo).HasMaxLength(50);
            entity.Property(e => e.PassportNo).HasMaxLength(50);
            entity.Property(e => e.PermanentAddessDetail).HasMaxLength(100);
            entity.Property(e => e.PhoneNo).HasMaxLength(50);
            entity.Property(e => e.RegistrationOnBs)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RegistrationOnBS");
            entity.Property(e => e.Remarks).HasColumnType("ntext");
            entity.Property(e => e.Son2).HasMaxLength(225);
            entity.Property(e => e.Son3).HasMaxLength(225);
            entity.Property(e => e.SonName).HasMaxLength(50);
            entity.Property(e => e.SpouseName).HasMaxLength(100);
            entity.Property(e => e.Street).HasMaxLength(50);
            entity.Property(e => e.SycStateVdcid).HasColumnName("SycStateVDCId");
            entity.Property(e => e.TemporaryAddressDetail).HasMaxLength(100);
            entity.Property(e => e.Tole).HasMaxLength(50);
            entity.Property(e => e.VdcmunicipalityNameInNepali).HasColumnName("VDCMunicipalityNameInNepali");
            entity.Property(e => e.VerifiedOn).HasColumnType("datetime");
            entity.Property(e => e.VotersId).HasMaxLength(50);
            entity.Property(e => e.WardNo).HasMaxLength(50);
            entity.Property(e => e.WaterSupplyNo).HasMaxLength(50);

            entity.HasOne(d => d.SycMemberGroup).WithMany(p => p.MemMemberRegistrations)
                .HasForeignKey(d => d.SycMemberGroupId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MemMemberRegistration_SycMemberGroup");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.MemMemberRegistrations)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MemMemberRegistration_UsmOffice");
        });

        modelBuilder.Entity<ShmShareType>(entity =>
        {
            entity.ToTable("ShmShareType");

            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CreatedOnBs).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_ShmShareTypeName_IsActive");
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LastModifiedOnBs).HasMaxLength(50);
            entity.Property(e => e.ShareFaceValue).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.SharePremium).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.ShareTypeName).HasMaxLength(50);
        });

        modelBuilder.Entity<SycCollectionCenter>(entity =>
        {
            entity.ToTable("SycCollectionCenter");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CollectionCenterName).HasMaxLength(255);
            entity.Property(e => e.CollectionCenterShortCode).HasMaxLength(20);
            entity.Property(e => e.ContactNo).HasMaxLength(20);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.DurationType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasComment("D=Days, W=Weeks, M=Months,  Y=Years");
            entity.Property(e => e.InstallmentType).HasMaxLength(3);
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LoanScheduleDateType).HasMaxLength(1);
            entity.Property(e => e.MeetingStartDateOn).HasColumnType("datetime");
            entity.Property(e => e.MeetingStartDateOnBs).HasMaxLength(10);
            entity.Property(e => e.MeetingTime).HasMaxLength(10);
            entity.Property(e => e.MemberYearlyPaymentAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.RegistrationOn).HasColumnType("datetime");
            entity.Property(e => e.RegistrationOnBs).HasMaxLength(10);
            entity.Property(e => e.SycVdcid).HasColumnName("SycVDCId");

            entity.HasOne(d => d.HurCollector).WithMany(p => p.SycCollectionCenters)
                .HasForeignKey(d => d.HurCollectorId)
                .HasConstraintName("FK_SycCollectionCenter_HurCollector");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.SycCollectionCenters)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SycCollectionCenter_UsmOffice");
        });

        modelBuilder.Entity<SycCompany>(entity =>
        {
            entity.HasKey(e => e.SycCompanyId).HasName("PK_dbo.SycCompany");

            entity.ToTable("SycCompany");

            entity.Property(e => e.SycCompanyId).ValueGeneratedNever();
            entity.Property(e => e.AppType).HasMaxLength(15);
            entity.Property(e => e.BroadCastId)
                .HasMaxLength(15)
                .HasDefaultValue("");
            entity.Property(e => e.ClientCode).HasMaxLength(10);
            entity.Property(e => e.CompanyAddress).HasMaxLength(200);
            entity.Property(e => e.CompanyAddressNepali).HasMaxLength(200);
            entity.Property(e => e.CompanyEmailId).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(200);
            entity.Property(e => e.CompanyNameNepali).HasMaxLength(200);
            entity.Property(e => e.CompanyWebSite).HasMaxLength(200);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.CurrentVersion).HasMaxLength(10);
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.FaxNo).HasMaxLength(100);
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LicenceIsActive).HasDefaultValue(true, "DF_SycCompany_LicenceIsActive");
            entity.Property(e => e.LicenseKey)
                .HasMaxLength(25)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NoReplyEmail).HasMaxLength(225);
            entity.Property(e => e.PanNo).HasMaxLength(100);
            entity.Property(e => e.PhoneNo).HasMaxLength(100);
            entity.Property(e => e.RegisteredNo).HasMaxLength(100);
            entity.Property(e => e.ServerKey)
                .HasMaxLength(255)
                .HasDefaultValue("");
        });

        modelBuilder.Entity<SycDepositType>(entity =>
        {
            entity.ToTable("SycDepositType");

            entity.Property(e => e.CollectorCommission).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.DepositTypeCode).HasMaxLength(20);
            entity.Property(e => e.DepositTypeName).HasMaxLength(200);
            entity.Property(e => e.DurationType).HasMaxLength(1);
            entity.Property(e => e.InterestActivationOn).HasColumnType("datetime");
            entity.Property(e => e.InterestActivationOnBs).HasMaxLength(50);
            entity.Property(e => e.InterestCalculationType)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValue("N", "DF_SycDepositType_InterestCalculationType");
            entity.Property(e => e.InterestRate).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_SycDepositType_IsActive");
            entity.Property(e => e.LastInterestTransferDateOnBs).HasMaxLength(10);
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.MaxPerDayWithDrawal).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.MaximumDepositAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.MinimumBalance).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.MinimumDepositForInterest).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.MinimumPerDayWithDrawal).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.MininumDepositAmount).HasColumnType("numeric(18, 2)");
            entity.Property(e => e.Remarks).HasColumnType("ntext");
            entity.Property(e => e.SycDepositCategoryId).HasComment("D=Days, M=Month, Y=Year");
            entity.Property(e => e.TaxRate).HasColumnType("numeric(18, 2)");
        });

        modelBuilder.Entity<SycMemberGroup>(entity =>
        {
            entity.ToTable("SycMemberGroup");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.ContactNo).HasMaxLength(100);
            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.GroupShortCode).HasMaxLength(20);
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.RegistrationOn).HasColumnType("datetime");
            entity.Property(e => e.RegistrationOnBs).HasMaxLength(50);

            entity.HasOne(d => d.SycCollectionCenter).WithMany(p => p.SycMemberGroups)
                .HasForeignKey(d => d.SycCollectionCenterId)
                .HasConstraintName("FK_SycMemberGroup_SycCollectionCenter");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.SycMemberGroups)
                .HasForeignKey(d => d.UsmOfficeId)
                .HasConstraintName("FK_SycMemberGroup_UsmOffice");
        });

        modelBuilder.Entity<UsmLogin>(entity =>
        {
            entity.ToTable("UsmLogin");

            entity.Property(e => e.LogOutOn).HasColumnType("datetime");
            entity.Property(e => e.LoginInOn).HasColumnType("datetime");
            entity.Property(e => e.SessionId).HasMaxLength(50);

            entity.HasOne(d => d.UsmUser).WithMany(p => p.UsmLogins)
                .HasForeignKey(d => d.UsmUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmLogin_UsmUser");
        });

        modelBuilder.Entity<UsmLoginActivity>(entity =>
        {
            entity.ToTable("UsmLoginActivity");

            entity.Property(e => e.ActivityOn).HasColumnType("datetime");
            entity.Property(e => e.ActivityOnBs).HasMaxLength(50);

            entity.HasOne(d => d.UsmLogin).WithMany(p => p.UsmLoginActivities)
                .HasForeignKey(d => d.UsmLoginId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmLoginActivity_UsmLogin");
        });

        modelBuilder.Entity<UsmOffice>(entity =>
        {
            entity.ToTable("UsmOffice");

            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.EmailAddress).HasMaxLength(100);
            entity.Property(e => e.FaxNumbers).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_UsmOffice_IsActive");
            entity.Property(e => e.JoinedOn).HasColumnType("datetime");
            entity.Property(e => e.JoinedOnBs)
                .HasMaxLength(50)
                .HasColumnName("JoinedOnBS");
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.OfficeDetailAddress).HasMaxLength(200);
            entity.Property(e => e.OfficeDetailAddressNepali).HasMaxLength(200);
            entity.Property(e => e.OfficeName).HasMaxLength(100);
            entity.Property(e => e.OfficeNameNepali).HasMaxLength(100);
            entity.Property(e => e.OfficeShortCode).HasMaxLength(50);
            entity.Property(e => e.OfficeUrl)
                .HasMaxLength(200)
                .HasColumnName("OfficeURL");
            entity.Property(e => e.PhoneNumbers).HasMaxLength(500);
            entity.Property(e => e.SycStateVdcid).HasColumnName("SycStateVDCId");
            entity.Property(e => e.SycVdcid).HasColumnName("SycVDCId");
            entity.Property(e => e.Tole).HasMaxLength(250);
            entity.Property(e => e.WardNo).HasMaxLength(50);
        });

        modelBuilder.Entity<UsmSystemEdition>(entity =>
        {
            entity.ToTable("UsmSystemEdition");

            entity.Property(e => e.UsmSystemEditionId).ValueGeneratedNever();
            entity.Property(e => e.Descrption).HasColumnType("ntext");
            entity.Property(e => e.SystemEditionName).HasMaxLength(100);
        });

        modelBuilder.Entity<UsmUser>(entity =>
        {
            entity.ToTable("UsmUser");

            entity.HasIndex(e => e.LoginEmailAddress, "IX_UsmUser").IsUnique();

            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_UsmUser_IsActive");
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.LoginEmailAddress).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(1000);
            entity.Property(e => e.PasswordChangedOn).HasColumnType("datetime");
            entity.Property(e => e.Remarks).HasColumnType("ntext");
            entity.Property(e => e.TellerLimit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UserCode).HasMaxLength(10);
            entity.Property(e => e.UserDetailAddress).HasMaxLength(200);
            entity.Property(e => e.UserPhoneNumber).HasMaxLength(100);
            entity.Property(e => e.WithdrawlLimit).HasColumnType("numeric(18, 2)");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.UsmUsers)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmUser_UsmOffice");

            entity.HasOne(d => d.UsmUserType).WithMany(p => p.UsmUsers)
                .HasForeignKey(d => d.UsmUserTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmUser_UsmUserType");

            // NOTE: skip-navigation UsingEntity<Dictionary<string,object>> blocks for
            // UsmRelationUserToOffice / UsmRelationUserToOfficeLogin removed from here.
            // They're configured as real entity types below instead — having both
            // caused the plural-table-name convention fallback that broke queries.
        });

        modelBuilder.Entity<UsmUserType>(entity =>
        {
            entity.ToTable("UsmUserType");

            entity.Property(e => e.CreatedOn).HasColumnType("datetime");
            entity.Property(e => e.Description).HasColumnType("ntext");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_UsmUserType_IsActive");
            entity.Property(e => e.LastModifiedOn).HasColumnType("datetime");
            entity.Property(e => e.UserTypeName).HasMaxLength(50);
        });

        // Configure UsmRelationUserToOffice as a real keyed entity with explicit table name
        modelBuilder.Entity<UsmRelationUserToOffice>(entity =>
        {
            entity.HasKey(e => new { e.UsmUserId, e.UsmOfficeId });
            entity.ToTable("UsmRelationUserToOffice");

            entity.HasOne(d => d.UsmUser)
                .WithMany(p => p.UsmRelationUserToOffices)
                .HasForeignKey(d => d.UsmUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmRelationUserToOffice_UsmUser");

            entity.HasOne(d => d.UsmOffice)
                .WithMany(p => p.UsmRelationUserToOffices)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmRelationUserToOffice_UsmOffice");
        });

        // Configure UsmRelationUserToOfficeLogin as a real keyed entity with explicit table name
        modelBuilder.Entity<UsmRelationUserToOfficeLogin>(entity =>
        {
            entity.HasKey(e => new { e.UsmUserId, e.UsmOfficeId });
            entity.ToTable("UsmRelationUserToOfficeLogin");

            entity.HasOne(d => d.UsmUser)
                .WithMany(p => p.UsmRelationUserToOfficeLogins)
                .HasForeignKey(d => d.UsmUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmRelationUserToOfficeLogin_UsmUser");

            entity.HasOne(d => d.UsmOffice)
                .WithMany(p => p.UsmRelationUserToOfficeLogins)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmRelationUserToOfficeLogin_UsmOffice");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}