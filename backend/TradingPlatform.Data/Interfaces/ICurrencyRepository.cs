using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Interfaces;

public interface ICurrencyRepository
{
    Task<CurrencyEntity?> GetByCodeAsync(string code);
    Task<List<CurrencyEntity>> GetAllAsync();
    Task<CurrencyEntity?> GetByIdAsync(Guid id);
    Task AddAsync(CurrencyEntity currency);
}
