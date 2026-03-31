namespace TradingPlatform.Data.Entities;

public sealed class MarketAssetEntity
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal ChangePercent { get; set; }
}
