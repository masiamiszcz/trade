using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Data.Repositories;

public sealed class InMemoryMarketDataRepository : IMarketDataRepository
{
    private static readonly IReadOnlyList<MarketAsset> Assets =
    [
        new("AAPL", "Apple", 214.32m, "USD", 1.42m),
        new("MSFT", "Microsoft", 428.11m, "USD", 0.87m),
        new("NVDA", "NVIDIA", 902.55m, "USD", 3.18m),
        new("TSLA", "Tesla", 181.09m, "USD", -1.24m)
    ];

    public Task<IReadOnlyList<MarketAsset>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Assets);

    public Task<MarketAsset?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
        => Task.FromResult(Assets.FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)));
}