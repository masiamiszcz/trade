using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Entities;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Context;

public sealed class TradingPlatformDbContext(DbContextOptions<TradingPlatformDbContext> options) : DbContext(options)
{
    public DbSet<MarketAssetEntity> MarketAssets => Set<MarketAssetEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<AdminEntity> Admins => Set<AdminEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<InstrumentEntity> Instruments => Set<InstrumentEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<AccountTransferEntity> AccountTransfers => Set<AccountTransferEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<AdminInvitationEntity> AdminInvitations => Set<AdminInvitationEntity>();
    public DbSet<AdminAuditLogEntity> AdminAuditLogs => Set<AdminAuditLogEntity>();
    public DbSet<AdminRequestEntity> AdminRequests => Set<AdminRequestEntity>();
    public DbSet<AdminRegistrationLogEntity> AdminRegistrationLogs => Set<AdminRegistrationLogEntity>();
    public DbSet<Core.Entities.ExchangeRateEntity> ExchangeRates => Set<Core.Entities.ExchangeRateEntity>();
    public DbSet<CandleEntity> Candles => Set<CandleEntity>();
    public DbSet<AssetMappingEntity> AssetMappings => Set<AssetMappingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingPlatformDbContext).Assembly);

        modelBuilder.Entity<CandleEntity>(entity =>
        {
            entity.HasIndex(c => new { c.Symbol, c.Source, c.IntervalMinutes, c.OpenTime })
                .HasDatabaseName("IX_Candles_Symbol_Source_Interval_OpenTime");
        });

        modelBuilder.Entity<AssetMappingEntity>(entity =>
        {
            entity.HasIndex(a => new { a.Symbol, a.CoingeckoId })
                .IsUnique()
                .HasDatabaseName("IX_AssetMappings_Symbol_CoingeckoId");
        });
    }
}
