namespace TradingPlatform.Core.Dtos;

public sealed record CfdChartDrawingPointDto(long Time, decimal Price);

public sealed record CfdChartDrawingDto(
    string Id,
    string Type,
    IReadOnlyList<CfdChartDrawingPointDto> Points,
    bool Locked);

public sealed record CfdChartViewConfigurationDto(
    int Version,
    IReadOnlyList<string> Symbols,
    string HighlightedSymbol,
    int RangeMinutes,
    int IntervalMinutes,
    string ChartType)
{
    public IReadOnlyList<CfdChartDrawingDto> Drawings { get; init; } = Array.Empty<CfdChartDrawingDto>();
}

public sealed record CfdChartViewDto(
    Guid Id,
    string Name,
    CfdChartViewConfigurationDto Configuration,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SaveCfdChartViewRequestDto(
    string Name,
    CfdChartViewConfigurationDto Configuration);
