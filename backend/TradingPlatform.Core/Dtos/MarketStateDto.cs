using System;

namespace TradingPlatform.Core.Dtos;

public class MarketStateDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal Volume { get; set; }
    public DateTime Timestamp { get; set; }
}
