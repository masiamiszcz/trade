using TradingPlatform.Core.Entities;

namespace TradingPlatform.Data.Repositories;

public interface IExchangeRateRepository
{
    Task AddAsync(ExchangeRateEntity entity);
    Task<ExchangeRateEntity?> GetLatestAsync(string baseCurrency, string quoteCurrency);
}
