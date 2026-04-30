namespace TradingPlatform.Core.Interfaces;

public interface IAssetMappingRepository
{
    Task<string?> GetCoingeckoIdBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
}
