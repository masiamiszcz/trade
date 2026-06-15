namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Repository for CryptoTransaction entities (immutable ledger of buy/sell operations)
/// </summary>
public interface ICryptoTransactionRepository
{
    /// <summary>
    /// Get transaction by ID
    /// </summary>
    Task<CryptoTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all transactions for an account with pagination
    /// </summary>
    Task<IEnumerable<CryptoTransaction>> GetByAccountIdAsync(
        Guid accountId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transaction count for account
    /// </summary>
    Task<int> GetCountByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions by symbol
    /// </summary>
    Task<IEnumerable<CryptoTransaction>> GetBySymbolAsync(
        Guid accountId,
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions filtered by type and status
    /// </summary>
    Task<IEnumerable<CryptoTransaction>> GetFilteredAsync(
        Guid accountId,
        string? type = null, // "Buy" or "Sell"
        string? status = null, // "Completed", "Rejected", "Pending"
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new transaction
    /// </summary>
    Task AddAsync(CryptoTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update transaction (for status changes, etc.)
    /// </summary>
    Task UpdateAsync(CryptoTransaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist changes to database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// CryptoTransaction entity - immutable ledger record of buy/sell operations
/// </summary>
public sealed record CryptoTransaction(
    Guid Id,
    Guid AccountId,
    Guid InstrumentId,
    string Symbol, // e.g. "BTCUSDT"
    string Type, // "Buy" or "Sell"
    decimal Quantity,
    decimal RequestedPrice,
    decimal ExecutionPrice,
    decimal TotalValue,
    string Status, // "Completed", "Rejected", "Pending"
    string Currency, // USD, EUR, etc.
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt = null,
    string? ErrorReason = null);
