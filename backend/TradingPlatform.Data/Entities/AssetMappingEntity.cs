namespace TradingPlatform.Data.Entities;

public class AssetMappingEntity
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public string CoingeckoId { get; set; } = "";
    public string Source { get; set; } = "coingecko";
}
