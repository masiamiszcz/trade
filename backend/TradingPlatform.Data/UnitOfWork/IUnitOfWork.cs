using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Data.UnitOfWork;

/// <summary>
/// Unit of Work pattern for managing database transactions
/// Ensures atomic operations across multiple repositories
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IAccountRepository Accounts { get; }
    IPositionRepository Positions { get; }
    ICryptoTransactionRepository CryptoTransactions { get; }
    IInstrumentRepository Instruments { get; }

    /// <summary>
    /// Begin database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commit all changes to database
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rollback all changes
    /// </summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
