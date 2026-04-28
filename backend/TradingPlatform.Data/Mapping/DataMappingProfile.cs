using AutoMapper;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Entities;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Mapping;

public class DataMappingProfile : Profile
{
    public DataMappingProfile()
    {
        // Currency mappings
        CreateMap<CurrencyEntity, CurrencyDto>().ReverseMap();

        // Exchange Rate mappings
        CreateMap<ExchangeRateEntity, ExchangeRateDto>().ReverseMap();
    }
}
