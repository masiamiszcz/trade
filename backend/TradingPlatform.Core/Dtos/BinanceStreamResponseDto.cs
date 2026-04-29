namespace TradingPlatform.Core.Dtos;

public class BinanceStreamResponseDto
{
    public string stream { get; set; } = string.Empty;
    public BinanceTradeDto? data { get; set; }
}
