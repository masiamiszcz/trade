namespace TradingPlatform.Core.Models;

public class CoingeckoSettings
{
    public string ApiBaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";
    public string AssetId { get; set; } = "bitcoin";
    public string VsCurrency { get; set; } = "usd";
    public int MarketChartDays { get; set; } = 1;
    public string Interval { get; set; } = "daily";
}
