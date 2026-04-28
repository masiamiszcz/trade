namespace TradingPlatform.Core.Dtos;

public class ExchangeRateDto
{
    public Guid Id { get; set; }
    public Guid BaseCurrencyId { get; set; }
    public Guid QuoteCurrencyId { get; set; }
    public decimal Rate { get; set; }
    public DateTime EffectiveAt { get; set; }
    public Guid SourceId { get; set; }
}
