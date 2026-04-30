using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Core.Interfaces;

public interface ICryptoService
{
    Task<IEnumerable<InstrumentDto>> GetAvailableCryptoInstrumentsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<CandleDto>> GetCandlesBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<CandleDto>> GetChartCandlesAsync(string symbol, int rangeMinutes, int? intervalMinutes = null, DateTime? to = null, CancellationToken cancellationToken = default);
}
