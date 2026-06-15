namespace TradingPlatform.Core.Dtos;

/// <summary>
/// Position in portfolio (holding in a specific crypto)
/// </summary>
public sealed record PortfolioPositionDto(
    string Symbol,
    decimal Quantity,
    decimal AverageOpenPrice,
    decimal CurrentPrice,
    decimal TotalValue,
    decimal PnL,
    decimal PnLPercent);

/// <summary>
/// Portfolio summary response for GET /api/cryptoportfolio/portfolio
/// Shows all positions and aggregate P&L
/// </summary>
public sealed record PortfolioSummaryDto(
    string BaseCurrency,
    IReadOnlyList<PortfolioPositionDto> Positions,
    PortfolioAggregateDto Summary);

/// <summary>
/// Aggregate portfolio metrics
/// </summary>
public sealed record PortfolioAggregateDto(
    decimal TotalValue,
    decimal TotalPnL,
    decimal TotalPnLPercent);
