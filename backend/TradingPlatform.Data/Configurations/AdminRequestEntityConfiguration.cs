namespace TradingPlatform.Data.Configurations;

// AdminRequestEntityConfiguration
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Core.Enums;
using TradingPlatform.Data.Entities;


/// <summary>
/// Entity Framework Core configuration for AdminRequestEntity.
/// Defines table structure, indexes, relationships, and constraints.
/// </summary>
public sealed class AdminRequestEntityConfiguration : IEntityTypeConfiguration<AdminRequestEntity>
{
    public void Configure(EntityTypeBuilder<AdminRequestEntity> builder)
    {
        // Table configuration
        builder.ToTable("AdminRequests");
        builder.HasKey(x => x.Id);

        // Property configurations
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.InstrumentId)
            .IsRequired();

        builder.Property(x => x.RequestedByAdminId)
            .IsRequired();

        builder.Property(x => x.ApprovedByAdminId)
            .IsRequired(false);

        builder.Property(x => x.Action)
            .HasMaxLength(50)
            .IsRequired()
            .HasConversion<string>();  // CRITICAL: Store enum as string name, not int value

        builder.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();  // CRITICAL: Store enum as string name, not int value

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.Property(x => x.ApprovedAtUtc)
            .IsRequired(false);

        // Indexes for query performance
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_AdminRequest_Status");

        builder.HasIndex(x => x.InstrumentId)
            .HasDatabaseName("IX_AdminRequest_InstrumentId");

        builder.HasIndex(x => x.RequestedByAdminId)
            .HasDatabaseName("IX_AdminRequest_RequestedByAdminId");

        builder.HasIndex(x => x.ApprovedByAdminId)
            .HasDatabaseName("IX_AdminRequest_ApprovedByAdminId");

        builder.HasIndex(x => x.CreatedAtUtc)
            .HasDatabaseName("IX_AdminRequest_CreatedAtUtc");

        // Relationships
        builder.HasOne(x => x.RequestedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.RequestedByAdminId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.ApprovedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Instrument)
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
