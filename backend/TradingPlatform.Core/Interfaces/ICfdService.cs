using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface ICfdService
{
    Task<IEnumerable<CfdInstrumentSummaryDto>> GetAvailableCfdInstrumentsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<CandleDto>> GetChartCandlesAsync(
        string symbol,
        int rangeMinutes,
        int? intervalMinutes = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
