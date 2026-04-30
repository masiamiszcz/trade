namespace TradingPlatform.Data.External;

public interface ICoingeckoApiClient
{
    Task<CoingeckoMarketChartResponse> GetMarketChartAsync(string assetId, string vsCurrency, int days, string interval, CancellationToken cancellationToken = default);
}
