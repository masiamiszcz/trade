using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        services.AddScoped<IMarketDataRepository, InMemoryMarketDataRepository>();
        return services;
    }
}