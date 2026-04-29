using System;

namespace TradingPlatform.Core.Dtos;

public class PriceUpdateDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
    public string UpdateId { get; set; } = Guid.NewGuid().ToString();
}
