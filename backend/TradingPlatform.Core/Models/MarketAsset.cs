namespace TradingPlatform.Core.Models;

public sealed record MarketAsset(
    string Symbol,
    string Name,
    decimal Price,
    string Currency,
    decimal ChangePercent);