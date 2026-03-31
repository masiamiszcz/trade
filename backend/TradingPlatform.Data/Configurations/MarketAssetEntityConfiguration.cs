using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Configurations;

public sealed class MarketAssetEntityConfiguration : IEntityTypeConfiguration<MarketAssetEntity>
{
    public void Configure(EntityTypeBuilder<MarketAssetEntity> builder)
    {
        builder.ToTable("MarketAssets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Symbol)
            .HasMaxLength(10)
            .IsRequired();

        builder.HasIndex(x => x.Symbol)
            .IsUnique();

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ChangePercent)
            .HasColumnType("decimal(5,2)");

        builder.HasData(
            new MarketAssetEntity
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Symbol = "AAPL",
                Name = "Apple",
                Price = 214.32m,
                Currency = "USD",
                ChangePercent = 1.42m
            },
            new MarketAssetEntity
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Symbol = "MSFT",
                Name = "Microsoft",
                Price = 428.11m,
                Currency = "USD",
                ChangePercent = 0.87m
            },
            new MarketAssetEntity
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Symbol = "NVDA",
                Name = "NVIDIA",
                Price = 902.55m,
                Currency = "USD",
                ChangePercent = 3.18m
            },
            new MarketAssetEntity
            {
                Id = new Guid("44444444-4444-4444-4444-444444444444"),
                Symbol = "TSLA",
                Name = "Tesla",
                Price = 181.09m,
                Currency = "USD",
                ChangePercent = -1.24m
            });
    }
}
