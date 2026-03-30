using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Services;

namespace TradingPlatform.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<IMarketDataService, MarketDataService>();
        return services;
    }
}