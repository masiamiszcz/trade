namespace TradingPlatform.Core.Dtos;

public class BinanceTradeDto
{
    public string e { get; set; } = string.Empty;
    public long E { get; set; }
    public string s { get; set; } = string.Empty;
    public long t { get; set; }
    public string p { get; set; } = string.Empty;
    public string q { get; set; } = string.Empty;
    public long T { get; set; }
    public bool m { get; set; }
}