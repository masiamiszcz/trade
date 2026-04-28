using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface ICurrencyService
{
    Task<CurrencyDto?> GetByCodeAsync(string code);
    Task<List<CurrencyDto>> GetAllAsync();
}
