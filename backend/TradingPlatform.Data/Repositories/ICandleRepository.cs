using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public interface ICandleRepository
{
    Task AddAsync(CandleEntity candle, CancellationToken cancellationToken = default);
    Task<List<CandleEntity>> GetBySymbolAsync(string symbol, int limit = 100, CancellationToken cancellationToken = default);
}
