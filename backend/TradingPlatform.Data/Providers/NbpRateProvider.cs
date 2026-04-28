using System.Net.Http.Json;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Data.Providers;

public class NbpRateProvider : INbpRateProvider
{
    private readonly HttpClient _http;

    public NbpRateProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<decimal> GetUsdToPlnRateAsync()
    {
        var response = await _http.GetFromJsonAsync<NbpResponse>(
            "https://api.nbp.pl/api/exchangerates/rates/A/USD/?format=json");

        if (response == null || response.Rates.Count == 0)
            throw new Exception("NBP error - no rates");

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
