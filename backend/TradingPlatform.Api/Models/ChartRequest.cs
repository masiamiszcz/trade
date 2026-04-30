namespace TradingPlatform.Api.Models;

public sealed record ChartRequest(
    int RangeMinutes = 43200,
    DateTime? To = null);
