using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;

namespace TradingPlatform.Tests;

public class MarketDataServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnAssetsFromRepository()
    {
        var repository = new FakeMarketDataRepository(
        [
            new MarketAsset("AAPL", "Apple", 214.32m, "USD", 1.42m),
            new MarketAsset("MSFT", "Microsoft", 428.11m, "USD", 0.87m)
        ]);

        var service = new MarketDataService(repository);

        var result = await service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("AAPL", result[0].Symbol);
    }

    [Fact]
    public async Task GetBySymbolAsync_ShouldIgnoreCase()
    {
        var repository = new FakeMarketDataRepository(
        [
            new MarketAsset("NVDA", "NVIDIA", 902.55m, "USD", 3.18m)
        ]);

        var service = new MarketDataService(repository);

        var result = await service.GetBySymbolAsync("nvda");

        Assert.NotNull(result);
        Assert.Equal("NVDA", result!.Symbol);
    }

    private sealed class FakeMarketDataRepository(List<MarketAsset> assets) : IMarketDataRepository
    {
        public Task<IReadOnlyList<MarketAsset>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MarketAsset>>(assets);

        public Task<MarketAsset?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
            => Task.FromResult(assets.FirstOrDefault(x => x.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)));
    }
}
