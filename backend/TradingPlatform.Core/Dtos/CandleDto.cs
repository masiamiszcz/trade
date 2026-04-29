namespace TradingPlatform.Core.Dtos;

public sealed record CandleDto(
    string Symbol,
    DateTime OpenTime,
    DateTime CloseTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);
