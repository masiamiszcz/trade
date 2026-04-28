using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public class RateSourceEntityConfiguration : IEntityTypeConfiguration<RateSourceEntity>
{
    public void Configure(EntityTypeBuilder<RateSourceEntity> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(r => r.Code)
            .IsUnique();
    }
}
