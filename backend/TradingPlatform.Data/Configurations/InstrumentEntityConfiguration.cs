using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Core.Enums;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class InstrumentEntityConfiguration : IEntityTypeConfiguration<InstrumentEntity>
{
    public void Configure(EntityTypeBuilder<InstrumentEntity> builder)
    {
        builder.ToTable("Instruments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(x => x.Symbol)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.BaseCurrency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.QuoteCurrency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasDefaultValueSql("SYSDATETIMEOFFSET()");

        builder.HasData(
            new InstrumentEntity
            {
                Id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Symbol = "AAPL",
                Name = "Apple Inc.",
                Description = "Apple Inc. stock",
                Type = InstrumentType.Stock,
                Pillar = AccountPillar.Stocks,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                Status = InstrumentStatus.Approved,
                IsBlocked = false,
                CreatedBy = Guid.Empty,
                CreatedAtUtc = new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero)
            },
            new InstrumentEntity
            {
                Id = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                Symbol = "BTCUSD",
                Name = "Bitcoin / US Dollar",
                Description = "Bitcoin cryptocurrency",
                Type = InstrumentType.Crypto,
                Pillar = AccountPillar.Crypto,
                BaseCurrency = "BTC",
                QuoteCurrency = "USD",
                Status = InstrumentStatus.Approved,
                IsBlocked = false,
                CreatedBy = Guid.Empty,
                CreatedAtUtc = new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero)
            },
            new InstrumentEntity
            {
                Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Symbol = "US500CFD",
                Name = "S&P 500 CFD",
                Description = "S&P 500 Contract for Difference",
                Type = InstrumentType.Cfd,
                Pillar = AccountPillar.Cfd,
                BaseCurrency = "USD",
                QuoteCurrency = "USD",
                Status = InstrumentStatus.Approved,
                IsBlocked = false,
                CreatedBy = Guid.Empty,
                CreatedAtUtc = new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero)
            });
    }
}
