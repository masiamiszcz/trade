namespace TradingPlatform.Core.Dtos;

public sealed record CfdInstrumentSummaryDto(
    InstrumentDto Instrument,
    decimal? CurrentPrice,
    decimal? Low1h,
    decimal? High1h,
    decimal? ChangePercent1h,
    decimal? Low24h,
    decimal? High24h,
    decimal? ChangePercent24h,
    IReadOnlyList<decimal> Sparkline7d,
    DateTime? CandleOpenTimeUtc);
