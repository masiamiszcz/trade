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
    /// Get all transactions for a user with pagination
    /// </summary>
    Task<IEnumerable<CryptoTransaction>> GetByUserIdAsync(
        Guid userId,
        int limit = 50,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transaction count for user
    /// </summary>
    Task<int> GetCountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions by user and symbol
    /// </summary>
    Task<IEnumerable<CryptoTransaction>> GetByUserAndSymbolAsync(
        Guid userId,
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transactions filtered by user, type and status
    /// </summary>
    Task<IEnumerable<CryptoTransaction>> GetFilteredByUserAsync(
        Guid userId,
        string? type = null,
        string? status = null,
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
    Guid UserId,
    Guid AccountId,
    Guid InstrumentId,
    string Symbol,
    string Type,
    decimal Quantity,
    decimal RequestedPrice,
    decimal ExecutionPrice,
    decimal TotalValue,
    string Status,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt = null,
    string? ErrorReason = null);
