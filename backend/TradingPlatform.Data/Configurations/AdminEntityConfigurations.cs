namespace TradingPlatform.Data.Configurations;

// AdminEntityConfigurations
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;


/// <summary>
/// Entity Framework configuration for admin invitations
/// </summary>
public sealed class AdminInvitationEntityConfiguration : IEntityTypeConfiguration<AdminInvitationEntity>
{
    public void Configure(EntityTypeBuilder<AdminInvitationEntity> builder)
    {
        builder.ToTable("AdminInvitations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => x.Token)
            .IsUnique();

        builder.Property(x => x.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.InvitedBy)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .IsRequired();

        builder.Property(x => x.UsedAt);

        builder.Property(x => x.UsedBy);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(AdminInvitationStatus.Pending);

        builder.Property(x => x.Permissions)
            .HasMaxLength(500);

        // Indexes for common queries
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ExpiresAt);

        // FK to Users (InvitedBy)
        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(x => x.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // FK to Users (UsedBy) - optional
        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(x => x.UsedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>
/// Entity Framework configuration for admin registration logs
/// </summary>
public sealed class AdminRegistrationLogEntityConfiguration : IEntityTypeConfiguration<AdminRegistrationLogEntity>
{
    public void Configure(EntityTypeBuilder<AdminRegistrationLogEntity> builder)
    {
        builder.ToTable("AdminRegistrationLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvitationId);

        builder.Property(x => x.AdminId);

        builder.Property(x => x.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasMaxLength(1000);

        // Indexes for queries
        builder.HasIndex(x => x.InvitationId);
        builder.HasIndex(x => x.AdminId);
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.CreatedAt);
    }
}

/// <summary>
/// Entity Framework configuration for admin audit logs
/// </summary>
public sealed class AdminAuditLogEntityConfiguration : IEntityTypeConfiguration<AdminAuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AdminAuditLogEntity> builder)
    {
        builder.ToTable("AdminAuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdminId)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Details)
            .HasMaxLength(1000);

        // FK to Users
        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for queries
        builder.HasIndex(x => x.AdminId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Action);
    }
}

/// <summary>
/// Entity Framework configuration for admin entity
/// Represents admin privileges for a user (1:1 relationship with Users)
/// </summary>
public sealed class AdminEntityConfiguration : IEntityTypeConfiguration<AdminEntity>
{
    public void Configure(EntityTypeBuilder<AdminEntity> builder)
    {
        builder.ToTable("Admins");

        // PK = FK to Users
        builder.HasKey(x => x.UserId);

        builder.Property(x => x.IsSuperAdmin)
            .IsRequired()
            .HasDefaultValue(false);

        // Index on IsSuperAdmin for quick lookups
        builder.HasIndex(x => x.IsSuperAdmin);

        // 1:1 relationship with Users
        // When a User is deleted, corresponding Admin record is deleted
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<AdminEntity>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
