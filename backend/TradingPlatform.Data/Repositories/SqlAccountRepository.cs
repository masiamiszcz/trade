using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlAccountRepository : IAccountRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlAccountRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Account?> GetMainAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Accounts.FirstOrDefaultAsync(
            x => x.UserId == userId && x.AccountType == AccountType.Main,
            cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Accounts
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        return entities.Select(MapToDomain);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        var entity = new AccountEntity
        {
            Id = account.Id,
            UserId = account.UserId,
            ParentAccountId = account.ParentAccountId,
            AccountNumber = account.AccountNumber,
            Name = account.Name,
            AccountType = account.AccountType,
            Pillar = account.Pillar,
            Status = account.Status,
            Currency = account.Currency,
            AvailableBalance = account.AvailableBalance,
            ReservedBalance = account.ReservedBalance,
            CreatedAtUtc = account.CreatedAtUtc
        };

        _dbContext.Accounts.Add(entity);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static Account MapToDomain(AccountEntity entity)
        => new Account(
            entity.Id,
            entity.UserId,
            entity.ParentAccountId,
            entity.AccountNumber,
            entity.Name,
            entity.AccountType,
            entity.Pillar,
            entity.Status,
            entity.Currency,
            entity.AvailableBalance,
            entity.ReservedBalance,
            entity.CreatedAtUtc);
}
