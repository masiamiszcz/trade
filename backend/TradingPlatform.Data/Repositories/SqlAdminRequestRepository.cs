
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

/// <summary>
/// SQL Server implementation of IAdminRequestRepository
/// Handles persistence of admin requests with two-step approval workflow
/// </summary>
public sealed class SqlAdminRequestRepository : IAdminRequestRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlAdminRequestRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AdminRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<AdminRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AdminRequests
            .AsNoTracking()
            .Where(x => x.Status == "Pending")
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<AdminRequest>> GetByInstrumentIdAsync(Guid instrumentId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AdminRequests
            .AsNoTracking()
            .Where(x => x.InstrumentId == instrumentId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<AdminRequest>> GetByRequestedByAdminIdAsync(Guid adminId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AdminRequests
            .AsNoTracking()
            .Where(x => x.RequestedByAdminId == adminId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<(IEnumerable<AdminRequest> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AdminRequests.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(MapToDomain), totalCount);
    }

    public async Task AddAsync(AdminRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new AdminRequestEntity
        {
            Id = request.Id,
            InstrumentId = request.InstrumentId,
            RequestedByAdminId = request.RequestedByAdminId,
            ApprovedByAdminId = request.ApprovedByAdminId,
            Action = request.Action,
            Reason = request.Reason,
            Status = request.Status,
            CreatedAtUtc = request.CreatedAtUtc,
            ApprovedAtUtc = request.ApprovedAtUtc
        };

        _dbContext.AdminRequests.Add(entity);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(AdminRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AdminRequests
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"AdminRequest with ID {request.Id} not found");

        entity.ApprovedByAdminId = request.ApprovedByAdminId;
        entity.Status = request.Status;
        entity.ApprovedAtUtc = request.ApprovedAtUtc;

        _dbContext.AdminRequests.Update(entity);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static AdminRequest MapToDomain(AdminRequestEntity entity)
        => new AdminRequest(
            entity.Id,
            entity.InstrumentId,
            entity.RequestedByAdminId,
            entity.ApprovedByAdminId,
            entity.Action,
            entity.Reason,
            entity.Status,
            entity.CreatedAtUtc,
            entity.ApprovedAtUtc);
}
