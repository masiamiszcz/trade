namespace TradingPlatform.Core.Interfaces;

public interface INbpRateProvider
{
    Task<decimal> GetUsdToPlnRateAsync();
}
    