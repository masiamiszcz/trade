using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class MarketDataService(IMarketDataRepository repository) : IMarketDataService
{
    public Task<IReadOnlyList<MarketAsset>> GetAllAsync(CancellationToken cancellationToken = default)
        => repository.GetAllAsync(cancellationToken);

    public Task<MarketAsset?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return Task.FromResult<MarketAsset?>(null);
        }

        return repository.GetBySymbolAsync(symbol.Trim().ToUpperInvariant(), cancellationToken);
    }
}