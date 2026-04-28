using TradingPlatform.Core.Entities;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateRepository _repo;

    public ExchangeRateService(IExchangeRateRepository repo)
    {
        _repo = repo;
    }

    public async Task SaveUsdRateAsync(decimal rate)
    {
        var entity = new ExchangeRateEntity
        {
            Id = Guid.NewGuid(),
            BaseCurrency = "USD",
            QuoteCurrency = "PLN",
            Rate = rate,
            Timestamp = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
    }

    public async Task<decimal?> GetUsdToPlnAsync()
    {
        var entity = await _repo.GetLatestAsync("USD", "PLN");
        return entity?.Rate;
    }
}
