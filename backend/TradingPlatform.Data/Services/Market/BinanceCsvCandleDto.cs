namespace TradingPlatform.Data.Services.Market;

public sealed record BinanceCsvCandleDto(
    long OpenTimeMs,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    long CloseTimeMs);
