namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Repository for Position entities (crypto holdings)
/// </summary>
public interface IPositionRepository
{
    /// <summary>
    /// Get specific position by account and instrument
    /// </summary>
    Task<Position?> GetByAccountAndInstrumentAsync(
        Guid accountId,
        Guid instrumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all positions for an account
    /// </summary>
    Task<IEnumerable<Position>> GetByAccountIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new position
    /// </summary>
    Task AddAsync(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing position
    /// </summary>
    Task UpdateAsync(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get by Symbol (string, e.g. "BTCUSDT") - helper for frontend
    /// </summary>
    Task<Position?> GetByAccountAndSymbolAsync(
        Guid accountId,
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist changes to database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Position entity - represents holding in a specific crypto asset
/// </summary>
public sealed record Position(
    Guid Id,
    Guid AccountId,
    Guid InstrumentId,
    string Symbol, // e.g. "BTCUSDT"
    decimal Quantity,
    decimal ReservedQuantity,
    decimal AverageOpenPrice,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);
