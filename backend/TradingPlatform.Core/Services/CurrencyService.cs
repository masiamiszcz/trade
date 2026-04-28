using AutoMapper;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Core.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _repository;

    public CurrencyService(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CurrencyDto?> GetByCodeAsync(string code)
    {
        return await _repository.GetByCodeAsync(code);
    }

    public async Task<List<CurrencyDto>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
}
