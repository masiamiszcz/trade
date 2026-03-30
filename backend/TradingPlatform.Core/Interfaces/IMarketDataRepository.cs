using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IMarketDataRepository
{
    Task<IReadOnlyList<MarketAsset>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MarketAsset?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}