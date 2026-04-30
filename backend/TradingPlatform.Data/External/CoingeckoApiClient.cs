using System.Collections.Generic;
using System.Net.Http.Json;

namespace TradingPlatform.Data.External;

public sealed class CoingeckoApiClient : ICoingeckoApiClient
{
    private readonly HttpClient _http;

    public CoingeckoApiClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
        }
    }

    public async Task<CoingeckoMarketChartResponse> GetMarketChartAsync(
        string assetId,
        string vsCurrency,
        int days,
        string interval,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"coins/{assetId}/market_chart?vs_currency={vsCurrency}&days={days}&interval={interval}";
        var response = await _http.GetFromJsonAsync<CoingeckoMarketChartResponse>(requestUri, cancellationToken);
        return response ?? new CoingeckoMarketChartResponse();
    }
}

public sealed class CoingeckoMarketChartResponse
{
    public List<List<decimal>> Prices { get; set; } = new();
    public List<List<decimal>> MarketCaps { get; set; } = new();
    public List<List<decimal>> TotalVolumes { get; set; } = new();
}
