using Microsoft.EntityFrameworkCore.Storage;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Data.UnitOfWork;

/// <summary>
/// Unit of Work implementation using DbContext transaction scope
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TradingPlatformDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public IAccountRepository Accounts { get; }
    public IPositionRepository Positions { get; }
    public ICryptoTransactionRepository CryptoTransactions { get; }
    public IInstrumentRepository Instruments { get; }

    public UnitOfWork(
        TradingPlatformDbContext dbContext,
        IAccountRepository accountRepository,
        IPositionRepository positionRepository,
        ICryptoTransactionRepository cryptoTransactionRepository,
        IInstrumentRepository instrumentRepository)
    {
        _dbContext = dbContext;
        Accounts = accountRepository;
        Positions = positionRepository;
        CryptoTransactions = cryptoTransactionRepository;
        Instruments = instrumentRepository;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (_transaction is not null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }
        await _dbContext.DisposeAsync();
    }
}
