using Microsoft.EntityFrameworkCore;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Context;

public sealed class TradingPlatformDbContext(DbContextOptions<TradingPlatformDbContext> options) : DbContext(options)
{
    public DbSet<MarketAssetEntity> MarketAssets => Set<MarketAssetEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<InstrumentEntity> Instruments => Set<InstrumentEntity>();
    public DbSet<PositionEntity> Positions => Set<PositionEntity>();
    public DbSet<AccountTransferEntity> AccountTransfers => Set<AccountTransferEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<AdminInvitationEntity> AdminInvitations => Set<AdminInvitationEntity>();
    public DbSet<AdminAuditLogEntity> AdminAuditLogs => Set<AdminAuditLogEntity>();
    public DbSet<AdminRequestEntity> AdminRequests => Set<AdminRequestEntity>();
    public DbSet<AdminRegistrationLogEntity> AdminRegistrationLogs => Set<AdminRegistrationLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingPlatformDbContext).Assembly);
    }
}
