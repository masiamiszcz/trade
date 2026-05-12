namespace TradingPlatform.Core.Interfaces;

public interface IExchangeRateService
{
    Task SaveUsdRateAsync(decimal rate);
    Task SaveEurRateAsync(decimal rate);
    Task SaveGbpRateAsync(decimal rate);
    Task<decimal?> GetUsdToPlnAsync();
    Task<decimal?> GetRateAsync(string from, string to);
}
