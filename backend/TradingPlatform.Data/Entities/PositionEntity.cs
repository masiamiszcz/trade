namespace TradingPlatform.Data.Entities;

public sealed class PositionEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid InstrumentId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageOpenPrice { get; set; }
    public decimal ReservedQuantity { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    
    /// <summary>
    /// Last time this position was updated (for sorting and audit)
    /// </summary>
    public DateTimeOffset LastUpdateTime { get; set; }
    
    /// <summary>
    /// Optimistic concurrency control - prevents race conditions on position updates during parallel BUY/SELL
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    public AccountEntity Account { get; set; } = null!;
    public InstrumentEntity Instrument { get; set; } = null!;
}
