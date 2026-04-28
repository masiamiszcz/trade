using System.Net.Http.Json;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Data.External;

/// <summary>
/// Single API client for all external rate providers
/// Extensible: add ECB, Binance, etc here
/// </summary>
public interface IExternalApiClient
{
    Task<decimal> GetUsdToPlnFromNbpAsync();
}

public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _http;

    public ExternalApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<decimal> GetUsdToPlnFromNbpAsync()
    {
        var response = await _http.GetFromJsonAsync<NbpResponse>(
            "https://api.nbp.pl/api/exchangerates/rates/A/USD/?format=json");

        if (response == null || response.Rates.Count == 0)
            throw new Exception("NBP error - no rates returned");

        return response.Rates[0].Mid;
    }

    private class NbpResponse
    {
        public List<NbpRate> Rates { get; set; } = new();
    }

    private class NbpRate
    {
        public decimal Mid { get; set; }
    }
}
