namespace TradingPlatform.Core.Dtos;

/// <summary>
/// Request to sell cryptocurrency
/// Frontend sends requestedPrice, backend fetches executionPrice from WebSocket and validates tolerance
/// </summary>
public sealed record SellCryptoRequest(
    string Symbol,
    decimal Quantity,
    decimal RequestedPrice,
    decimal TolerancePercent = 1.0m);
