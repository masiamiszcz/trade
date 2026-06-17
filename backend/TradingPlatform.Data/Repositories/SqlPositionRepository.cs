using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlPositionRepository : IPositionRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlPositionRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Position?> GetByAccountAndInstrumentAsync(
        Guid accountId,
        Guid instrumentId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Positions
            .Where(p => p.AccountId == accountId && p.InstrumentId == instrumentId)
            .Include(p => p.Instrument)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<Position>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Positions
            .Where(p => p.UserId == userId)
            .Include(p => p.Instrument)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<Position?> GetByUserAndSymbolAsync(
        Guid userId,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Positions
            .Where(p => p.UserId == userId && p.Instrument.Symbol == symbol)
            .Include(p => p.Instrument)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task AddAsync(Position position, CancellationToken cancellationToken = default)
    {
        var entity = new PositionEntity
        {
            Id = position.Id,
            UserId = position.UserId,
            AccountId = position.AccountId,
            InstrumentId = position.InstrumentId,
            Quantity = position.Quantity,
            ReservedQuantity = position.ReservedQuantity,
            AverageOpenPrice = position.AverageOpenPrice,
            OpenedAtUtc = position.CreatedAt,
            UpdatedAtUtc = position.LastUpdatedAt
        };

        _dbContext.Positions.Add(entity);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Position position, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Positions.FirstOrDefaultAsync(
            p => p.Id == position.Id,
            cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"Position {position.Id} not found");

        entity.Quantity = position.Quantity;
        entity.ReservedQuantity = position.ReservedQuantity;
        entity.AverageOpenPrice = position.AverageOpenPrice;
        entity.UpdatedAtUtc = position.LastUpdatedAt;

        _dbContext.Positions.Update(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Position MapToDomain(PositionEntity entity)
    {
        return new Position(
            Id: entity.Id,
            UserId: entity.UserId,
            AccountId: entity.AccountId,
            InstrumentId: entity.InstrumentId,
            Symbol: entity.Instrument.Symbol,
            Quantity: entity.Quantity,
            ReservedQuantity: entity.ReservedQuantity,
            AverageOpenPrice: entity.AverageOpenPrice,
            CreatedAt: entity.OpenedAtUtc,
            LastUpdatedAt: entity.UpdatedAtUtc ?? entity.OpenedAtUtc);
    }
}
