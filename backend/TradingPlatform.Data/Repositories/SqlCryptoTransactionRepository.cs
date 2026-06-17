using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlCryptoTransactionRepository : ICryptoTransactionRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlCryptoTransactionRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CryptoTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CryptoTransactions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<CryptoTransaction>> GetByUserIdAsync(
        Guid userId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.CryptoTransactions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<int> GetCountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CryptoTransactions
            .Where(x => x.UserId == userId)
            .CountAsync(cancellationToken);
    }

    public async Task<IEnumerable<CryptoTransaction>> GetByUserAndSymbolAsync(
        Guid userId,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.CryptoTransactions
            .Where(x => x.UserId == userId && x.Symbol == symbol)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<CryptoTransaction>> GetFilteredByUserAsync(
        Guid userId,
        string? type = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CryptoTransactions
            .Where(x => x.UserId == userId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(x => x.Type == type);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var entities = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain);
    }

    public async Task AddAsync(CryptoTransaction transaction, CancellationToken cancellationToken = default)
    {
        var entity = new CryptoTransactionEntity
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            AccountId = transaction.AccountId,
            InstrumentId = transaction.InstrumentId,
            Symbol = transaction.Symbol,
            Type = transaction.Type,
            Quantity = transaction.Quantity,
            RequestedPrice = transaction.RequestedPrice,
            ExecutionPrice = transaction.ExecutionPrice,
            TotalValue = transaction.TotalValue,
            Status = transaction.Status,
            Currency = transaction.Currency,
            CreatedAt = transaction.CreatedAt,
            CompletedAt = transaction.CompletedAt,
            ErrorReason = transaction.ErrorReason
        };

        _dbContext.CryptoTransactions.Add(entity);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(CryptoTransaction transaction, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.CryptoTransactions
            .FirstOrDefaultAsync(x => x.Id == transaction.Id, cancellationToken);

        if (entity is null)
            throw new InvalidOperationException($"CryptoTransaction {transaction.Id} not found");

        entity.Status = transaction.Status;
        entity.ExecutionPrice = transaction.ExecutionPrice;
        entity.CompletedAt = transaction.CompletedAt;
        entity.ErrorReason = transaction.ErrorReason;

        _dbContext.CryptoTransactions.Update(entity);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static CryptoTransaction MapToDomain(CryptoTransactionEntity entity)
    {
        return new CryptoTransaction(
            Id: entity.Id,
            UserId: entity.UserId,
            AccountId: entity.AccountId,
            InstrumentId: entity.InstrumentId,
            Symbol: entity.Symbol,
            Type: entity.Type,
            Quantity: entity.Quantity,
            RequestedPrice: entity.RequestedPrice,
            ExecutionPrice: entity.ExecutionPrice,
            TotalValue: entity.TotalValue,
            Status: entity.Status,
            Currency: entity.Currency,
            CreatedAt: entity.CreatedAt,
            CompletedAt: entity.CompletedAt,
            ErrorReason: entity.ErrorReason);
    }
}
