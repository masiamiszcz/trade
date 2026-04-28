namespace TradingPlatform.Core.Interfaces;

public interface IExchangeRateService
{
    Task SaveUsdRateAsync(decimal rate);
    Task<decimal?> GetUsdToPlnAsync();
}
