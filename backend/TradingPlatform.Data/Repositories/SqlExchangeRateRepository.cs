using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Entities;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Data.Repositories;

public class SqlExchangeRateRepository : IExchangeRateRepository
{
    private readonly TradingPlatformDbContext _db;

    public SqlExchangeRateRepository(TradingPlatformDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ExchangeRateEntity entity)
    {
        _db.ExchangeRates.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task<ExchangeRateEntity?> GetLatestAsync(string baseCurrency, string quoteCurrency)
    {
        return await _db.ExchangeRates
            .Where(x => x.BaseCurrency == baseCurrency && x.QuoteCurrency == quoteCurrency)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();
    }
}
