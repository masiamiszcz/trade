namespace TradingPlatform.Data.Configurations;

// AuditLogEntityConfiguration
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;


/// <summary>
/// Entity Framework Core configuration for AuditLogEntity.
/// Defines immutable audit log table structure and indexes for compliance.
/// </summary>
public sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        // Table configuration
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        // Immutable table - no updates allowed at database level
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.AdminId)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.EntityId)
            .IsRequired(false);

        builder.Property(x => x.Details)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45) // IPv4: max 15 chars, IPv6: max 39 chars
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        // Indexes for audit trail queries
        builder.HasIndex(x => x.AdminId)
            .HasDatabaseName("IX_AuditLog_AdminId");

        builder.HasIndex(x => x.Action)
            .HasDatabaseName("IX_AuditLog_Action");

        builder.HasIndex(x => x.EntityType)
            .HasDatabaseName("IX_AuditLog_EntityType");

        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("IX_AuditLog_CreatedAtUtc");

        // Composite index for common queries
        builder.HasIndex(x => new { x.AdminId, x.CreatedAtUtc })
            .HasDatabaseName("IX_AuditLog_AdminId_CreatedAtUtc");

        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc })
            .HasDatabaseName("IX_AuditLog_Entity_CreatedAtUtc");

        // Relationship
        builder.HasOne(x => x.Admin)
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
