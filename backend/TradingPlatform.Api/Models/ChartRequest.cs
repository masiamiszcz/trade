namespace TradingPlatform.Api.Models;

public sealed record ChartRequest(
    int RangeMinutes = 43200,
    int? IntervalMinutes = null,
    DateTime? To = null,
    string? UserBaseCurrency = null);
