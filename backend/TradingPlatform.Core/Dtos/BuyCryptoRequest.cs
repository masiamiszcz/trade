namespace TradingPlatform.Core.Dtos;

/// <summary>
/// Request to buy cryptocurrency
/// Frontend sends requestedPrice, backend fetches executionPrice from WebSocket and validates tolerance
/// </summary>
public sealed record BuyCryptoRequest(
    string Symbol,
    decimal Quantity,
    decimal RequestedPrice,
    decimal TolerancePercent = 1.0m);
