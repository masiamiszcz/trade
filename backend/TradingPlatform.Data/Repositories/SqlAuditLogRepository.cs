
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

/// <summary>
/// SQL Server implementation of IAuditLogRepository
/// Handles persistence of immutable audit trail entries
/// </summary>
public sealed class SqlAuditLogRepository : IAuditLogRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlAuditLogRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<AuditLog>> GetByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.AdminId == adminId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<AuditLog>> GetByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.Action == action)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(MapToDomain), totalCount);
    }

    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        var entity = new AuditLogEntity
        {
            Id = auditLog.Id,
            AdminId = auditLog.AdminId,
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            Details = auditLog.Details,
            IpAddress = auditLog.IpAddress,
            CreatedAtUtc = auditLog.CreatedAtUtc
        };

        _dbContext.AuditLogs.Add(entity);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static AuditLog MapToDomain(AuditLogEntity entity)
        => new AuditLog(
            entity.Id,
            entity.AdminId,
            entity.Action,
            entity.EntityType,
            entity.EntityId,
            entity.Details,
            entity.IpAddress,
            entity.CreatedAtUtc);
}
