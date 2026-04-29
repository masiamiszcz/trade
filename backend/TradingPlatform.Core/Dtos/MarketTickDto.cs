using System;

namespace TradingPlatform.Core.Dtos;

public class MarketTickDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsBuyerMaker { get; set; }
}
