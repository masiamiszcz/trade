using Microsoft.EntityFrameworkCore;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Context;

public sealed class TradingPlatformDbContext(DbContextOptions<TradingPlatformDbContext> options) : DbContext(options)
{
    public DbSet<MarketAssetEntity> MarketAssets => Set<MarketAssetEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingPlatformDbContext).Assembly);
    }
}
