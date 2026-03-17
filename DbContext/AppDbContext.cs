using System;
using System.Collections.Generic;
using JsSampleReport.Models;
using Microsoft.EntityFrameworkCore;

namespace JsSampleReport;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MemMemberRegistration> MemMemberRegistrations { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseSqlServer("Server=DESKTOP-G41KGSS\\SQLEXPRESS;Database=NexGenCoSysDBDev;Persist Security Info=True;User ID=SA;Password=cosys123;TrustServerCertificate=True;Encrypt=False");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MemMemberRegistration>(entity =>
        {
            entity.ToTable("MemMemberRegistration");

            entity.HasIndex(e => e.MemberId, "IX_MemMemberRegistration").IsUnique();

            entity.Property(e => e.BirthOnBs)
                .HasMaxLength(20)
                .HasColumnName("BirthOnBS");
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
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
