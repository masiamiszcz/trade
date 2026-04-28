namespace TradingPlatform.Core.Entities;

public class ExchangeRateEntity
{
    public Guid Id { get; set; }
    
    public string BaseCurrency { get; set; } = null!;  // "USD"
    public string QuoteCurrency { get; set; } = null!; // "PLN"
    
    public decimal Rate { get; set; }
    
    public DateTime Timestamp { get; set; }
}
