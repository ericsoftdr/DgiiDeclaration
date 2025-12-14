using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DgiiIntegration.Models;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountingManager> AccountingManagers { get; set; }

    public virtual DbSet<CompanyCredential> CompanyCredentials { get; set; }

    public virtual DbSet<CompanyCredentialToken> CompanyCredentialTokens { get; set; }

    public virtual DbSet<CompanyCredentialTokensCopy> CompanyCredentialTokensCopies { get; set; }

    public virtual DbSet<Tmp> Tmps { get; set; }

    public virtual DbSet<TmpCompany> TmpCompanies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountingManager>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Pk_AccountingManager");

            entity.ToTable("AccountingManager");

            entity.Property(e => e.BusinessName)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ManagerName)
                .HasMaxLength(128)
                .IsUnicode(false);
        });

        modelBuilder.Entity<CompanyCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Pk_CompanyCredentials");

            entity.HasIndex(e => e.Rnc, "Uq_Rnc").IsUnique();

            entity.Property(e => e.CompanyName)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.DateProcessed).HasColumnType("datetime");
            entity.Property(e => e.FileType)
                .HasMaxLength(280)
                .IsUnicode(false);
            entity.Property(e => e.Pwd)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Rnc)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.StatusInd).HasDefaultValue(true);

            entity.HasOne(d => d.AccountingManager).WithMany(p => p.CompanyCredentials)
                .HasForeignKey(d => d.AccountingManagerId)
                .HasConstraintName("Fk_AccountingManager_On_CompanyCredentials");
        });

        modelBuilder.Entity<CompanyCredentialToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Pk_CompanyCredentialTokens");

            entity.HasIndex(e => new { e.CompanyCredentialId, e.TokenId }, "Uq_CompanyCredentialId_TokenId").IsUnique();

            entity.Property(e => e.TokenValue)
                .HasMaxLength(5)
                .IsUnicode(false);

            entity.HasOne(d => d.CompanyCredential).WithMany(p => p.CompanyCredentialTokens)
                .HasForeignKey(d => d.CompanyCredentialId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("Fk_CompanyCredentials_On_CompanyCredentialTokens");
        });

        modelBuilder.Entity<CompanyCredentialTokensCopy>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CompanyCredentialTokensCopy");

            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.TokenValue)
                .HasMaxLength(5)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Tmp>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("Tmp");

            entity.Property(e => e.Rnc)
                .HasMaxLength(11)
                .IsUnicode(false);
            entity.Property(e => e.TokenValue)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TmpCompany>(entity =>
        {
            entity.HasNoKey();

            entity.Property(e => e.CompanyName)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Pwd)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Rnc)
                .HasMaxLength(11)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
