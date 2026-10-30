using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class CfdChartViewEntityConfiguration : IEntityTypeConfiguration<CfdChartViewEntity>
{
    public void Configure(EntityTypeBuilder<CfdChartViewEntity> builder)
    {
        builder.ToTable("CfdChartViews");
        builder.HasKey(view => view.Id);

        builder.Property(view => view.Name)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(view => view.ConfigurationJson)
            .HasMaxLength(16_384)
            .IsRequired();

        builder.Property(view => view.ConfigurationVersion)
            .IsRequired();

        builder.Property(view => view.CreatedAtUtc)
            .IsRequired();

        builder.Property(view => view.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(view => new { view.UserId, view.UpdatedAtUtc });
        builder.HasOne<UserEntity>()
            .WithMany()
            .HasForeignKey(view => view.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
