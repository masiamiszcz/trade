using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;

namespace TradingPlatform.Data.External;

public sealed class BinanceApiClient : IBinanceApiClient
{
    private readonly HttpClient _http;

    public BinanceApiClient(HttpClient http)
    {
        _http = http;
        if (_http.BaseAddress == null)
        {
            _http.BaseAddress = new Uri("https://api.binance.com/");
        }
    }

    public async Task<IEnumerable<BinanceKline>> GetHistoricalKlinesAsync(
        string symbol,
        string interval,
        int limit,
        DateTime? startTime = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";

        if (startTime.HasValue)
        {
            var startTimeMs = new DateTimeOffset(startTime.Value.ToUniversalTime()).ToUnixTimeMilliseconds();
            requestUri += $"&startTime={startTimeMs}";
        }

        var response = await _http.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<BinanceKline>();
        }

        var klines = new List<BinanceKline>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Array || item.GetArrayLength() < 6)
            {
                continue;
            }

            var openTime = item[0].GetInt64();
            var open = ParseDecimal(item[1]);
            var high = ParseDecimal(item[2]);
            var low = ParseDecimal(item[3]);
            var close = ParseDecimal(item[4]);
            var volume = ParseDecimal(item[5]);
            var closeTime = item[6].GetInt64();

            klines.Add(new BinanceKline(
                DateTimeOffset.FromUnixTimeMilliseconds(openTime).UtcDateTime,
                open,
                high,
                low,
                close,
                volume,
                DateTimeOffset.FromUnixTimeMilliseconds(closeTime).UtcDateTime));
        }

        return klines;
    }

    private static decimal ParseDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), out var value))
        {
            return value;
        }

        return element.GetDecimal();
    }
}

public sealed record BinanceKline(
    DateTime OpenTime,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume,
    DateTime CloseTime);
