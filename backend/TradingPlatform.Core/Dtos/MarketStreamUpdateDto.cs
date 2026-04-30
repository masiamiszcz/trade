using System;

namespace TradingPlatform.Core.Dtos;

public sealed record MarketStreamUpdateDto(
    PriceUpdateDto? Tick,
    CandleDto? Candle);
