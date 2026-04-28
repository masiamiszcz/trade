namespace TradingPlatform.Core.Dtos;

public class CurrencyDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!; // USD, EUR, PLN
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
}
