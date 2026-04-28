using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface IEcbRateProvider
{
    Task<List<ExchangeRateDto>> FetchAsync();
}
