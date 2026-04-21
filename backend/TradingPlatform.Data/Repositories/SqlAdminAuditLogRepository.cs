using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

/// <summary>
/// SQL Server implementation of IAdminAuditLogRepository
/// Persists admin security audit logs
/// </summary>
public sealed class SqlAdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlAdminAuditLogRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<dynamic>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.Cast<dynamic>().ToList();
    }

    public async Task<IEnumerable<dynamic>> GetByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.AdminAuditLogs
            .Where(x => x.AdminId == adminId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return results.Cast<dynamic>().ToList();
    }

    public async Task<IEnumerable<dynamic>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var results = await _dbContext.AdminAuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
        return results.Cast<dynamic>().ToList();
    }

    public async Task AddAsync(dynamic log, CancellationToken cancellationToken = default)
    {
        if (log is AdminAuditLogEntity entity)
        {
            await _dbContext.AdminAuditLogs.AddAsync(entity, cancellationToken);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
