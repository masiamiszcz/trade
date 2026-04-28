using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.External;

namespace TradingPlatform.Data.Services;

public class RateFetcherHostedService : BackgroundService
{
    private readonly IExternalApiClient _api;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RateFetcherHostedService> _logger;

    public RateFetcherHostedService(IExternalApiClient api, IServiceProvider serviceProvider, ILogger<RateFetcherHostedService> logger)
    {
        _api = api;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for database migrations to complete (increase to 30s to ensure migration runs)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching USD/PLN from NBP...");
                
                var rate = await _api.GetUsdToPlnFromNbpAsync();
                _logger.LogInformation("NBP response: {Rate}", rate);
                
                // Create scope for scoped service
                using (var scope = _serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
                    await service.SaveUsdRateAsync(rate);
                }
                
                _logger.LogInformation("USD/PLN saved: {Rate}", rate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}