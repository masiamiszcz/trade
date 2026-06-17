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
    /// Get all positions for a user
    /// </summary>
    Task<IEnumerable<Position>> GetByUserIdAsync(
        Guid userId,
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
    /// Get by user and symbol
    /// </summary>
    Task<Position?> GetByUserAndSymbolAsync(
        Guid userId,
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
    Guid UserId,
    Guid AccountId,
    Guid InstrumentId,
    string Symbol,
    decimal Quantity,
    decimal ReservedQuantity,
    decimal AverageOpenPrice,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);
