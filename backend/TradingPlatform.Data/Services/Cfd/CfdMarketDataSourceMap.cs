namespace TradingPlatform.Data.Services.Cfd;

public sealed record CfdMarketDataSource(string Provider, string MarketSymbol);

public static class CfdMarketDataSourceMap
{
    private static readonly IReadOnlyDictionary<string, CfdMarketDataSource> Sources =
        new Dictionary<string, CfdMarketDataSource>(StringComparer.OrdinalIgnoreCase)
        {
            ["BTCUSDT"] = new("binance", "BTCUSDT")
        };

    public static bool TryGetSource(string instrumentSymbol, out CfdMarketDataSource source)
    {
        if (string.IsNullOrWhiteSpace(instrumentSymbol))
        {
            source = null!;
            return false;
        }

        return Sources.TryGetValue(instrumentSymbol.Trim(), out source!);
    }
}
