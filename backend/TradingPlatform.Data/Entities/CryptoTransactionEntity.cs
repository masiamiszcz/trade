namespace TradingPlatform.Data.Entities;

/// <summary>
/// CryptoTransactionEntity - Immutable ledger of all buy/sell operations
/// Records both requested and execution prices for compliance and audit trail
/// </summary>
public sealed class CryptoTransactionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid AccountId { get; set; }
    public Guid InstrumentId { get; set; }
    
    /// <summary>
    /// Symbol (e.g. "BTCUSDT") - denormalized from InstrumentEntity for quick queries
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Type: "Buy" or "Sell"
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Quantity of crypto being traded
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Price sent by frontend (what user saw on screen)
    /// </summary>
    public decimal RequestedPrice { get; set; }
    
    /// <summary>
    /// Actual price used from WebSocket at execution time
    /// NEVER the same as RequestedPrice - always fetched fresh from market data
    /// </summary>
    public decimal ExecutionPrice { get; set; }
    
    /// <summary>
    /// Total value = Quantity × ExecutionPrice
    /// </summary>
    public decimal TotalValue { get; set; }
    
    /// <summary>
    /// Status: "Completed", "Rejected", "Pending"
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// Currency (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// When transaction was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// When transaction was completed or rejected
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
    
    /// <summary>
    /// Error reason if transaction was rejected (e.g. "Insufficient balance", "Price out of tolerance")
    /// </summary>
    public string? ErrorReason { get; set; }

    // Navigation properties
    public AccountEntity Account { get; set; } = null!;
    public InstrumentEntity Instrument { get; set; } = null!;
}
