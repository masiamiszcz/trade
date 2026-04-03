using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Services;

namespace TradingPlatform.Tests;

public class DatabaseFixture : IDisposable
{
    public IServiceProvider Services { get; }

    public DatabaseFixture()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TradingPlatformDbContext>(options =>
            options.UseInMemoryDatabase("TestDatabase"));

        services.AddScoped<IHealthService, HealthService>();

        Services = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}