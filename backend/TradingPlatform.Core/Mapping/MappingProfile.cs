using AutoMapper;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<Account, AccountDto>()
            .ForMember(d => d.AccountType, opt => opt.MapFrom(s => s.AccountType.ToString()))
            .ForMember(d => d.Pillar, opt => opt.MapFrom(s => s.Pillar.ToString()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()));
    }
}
