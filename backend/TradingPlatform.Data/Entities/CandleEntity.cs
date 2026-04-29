namespace TradingPlatform.Data.Entities;

public class CandleEntity
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public DateTime OpenTime { get; set; }
    public DateTime CloseTime { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
