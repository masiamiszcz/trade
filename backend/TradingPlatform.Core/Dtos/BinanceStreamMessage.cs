using System.Text.Json.Serialization;

namespace TradingPlatform.Core.Dtos;

public class BinanceStreamMessage
{
    [JsonPropertyName("s")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("p")]
    public string Price { get; set; } = string.Empty;

    [JsonPropertyName("q")]
    public string Quantity { get; set; } = string.Empty;

    [JsonPropertyName("T")]
    public long TradeTime { get; set; }

    [JsonPropertyName("m")]
    public bool IsBuyerMaker { get; set; }

    [JsonPropertyName("t")]
    public long TradeId { get; set; }
}
