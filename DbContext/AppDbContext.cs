using Microsoft.EntityFrameworkCore;
using NexgenCosysReport.Models;

namespace NexgenCosysReport;

public partial class AppDbContext : DbContext
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

    public virtual DbSet<MemMemberRegistration> MemMemberRegistrations { get; set; }

    public virtual DbSet<SycCollectionCenter> SycCollectionCenters { get; set; }

    public virtual DbSet<SycDepositType> SycDepositTypes { get; set; }

    public virtual DbSet<SycMemberGroup> SycMemberGroups { get; set; }

    public virtual DbSet<UsmOffice> UsmOffices { get; set; }

    public virtual DbSet<UsmRelationUserToOffice> UsmRelationUserToOffices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }
    //#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    //        => optionsBuilder.UseSqlServer("Server=DESKTOP-G41KGSS\\SQLEXPRESS;Database=NexGenCoSysDBDev;Persist Security Info=True;User ID=SA;Password=cosys123;TrustServerCertificate=True;Encrypt=False");

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

        modelBuilder.Entity<UsmRelationUserToOffice>(entity =>
        {
            entity.HasKey(e => new { e.UsmUserId, e.UsmOfficeId });

            entity.ToTable("UsmRelationUserToOffice");

            entity.HasOne(d => d.UsmOffice).WithMany(p => p.UsmRelationUserToOffices)
                .HasForeignKey(d => d.UsmOfficeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsmRelationUserToOffice_UsmOffice");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
