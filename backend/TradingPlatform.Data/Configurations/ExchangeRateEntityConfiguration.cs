using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Core.Entities;

namespace TradingPlatform.Data.Configurations;

public class ExchangeRateEntityConfiguration : IEntityTypeConfiguration<ExchangeRateEntity>
{
    public void Configure(EntityTypeBuilder<ExchangeRateEntity> builder)
    {
        builder.ToTable("ExchangeRates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BaseCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.QuoteCurrency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(e => e.Rate)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .IsRequired();

        // Index for efficient lookups by currency pair and timestamp
        builder.HasIndex(e => new { e.BaseCurrency, e.QuoteCurrency, e.Timestamp });
    }
}
