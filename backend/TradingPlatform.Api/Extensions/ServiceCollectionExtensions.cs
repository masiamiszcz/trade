using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Api.Services;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Services;
using TradingPlatform.Data.Services.Cfd;

namespace TradingPlatform.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BinanceSettings>(configuration.GetSection("Binance"));
        services.AddSignalR();
        services.AddSingleton<IPriceUpdatePublisher, PriceUpdatePublisher>();
        services.AddScoped<ICryptoService, CryptoService>();
        services.AddScoped<ICfdService, CfdService>();
        services.AddScoped<ICfdChartViewService, CfdChartViewService>();
        return services;
    }
}
