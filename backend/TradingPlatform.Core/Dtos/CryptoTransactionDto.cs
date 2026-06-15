namespace TradingPlatform.Core.Dtos;

/// <summary>
/// Response DTO for crypto buy/sell transaction
/// Contains both requested and execution prices for audit trail
/// </summary>
public sealed record CryptoTransactionDto(
    Guid Id,
    string Symbol,
    string Type, // "Buy" or "Sell"
    decimal Quantity,
    decimal RequestedPrice,
    decimal ExecutionPrice,
    decimal TotalValue,
    string Status, // "Completed", "Rejected", "Pending"
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt = null,
    string? ErrorMessage = null);
