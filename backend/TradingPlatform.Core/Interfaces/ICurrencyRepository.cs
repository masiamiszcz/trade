using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface ICurrencyRepository
{
    Task<CurrencyDto?> GetByCodeAsync(string code);
    Task<List<CurrencyDto>> GetAllAsync();
    Task<CurrencyDto?> GetByIdAsync(Guid id);
    Task AddAsync(CurrencyDto currency);
}

