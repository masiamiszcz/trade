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
        _logger.LogInformation("RateFetcherHostedService starting...");
        
        // Wait for database migrations to complete (increase to 30s to ensure migration runs)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        
        _logger.LogInformation("RateFetcherHostedService started, entering main loop");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching exchange rates from NBP...");

                // USD/PLN
                var usdRate = await _api.GetUsdToPlnFromNbpAsync();
                _logger.LogInformation("NBP USD/PLN: {Rate}", usdRate);

                // EUR/PLN
                var eurRate = await _api.GetEurToPlnFromNbpAsync();
                _logger.LogInformation("NBP EUR/PLN: {Rate}", eurRate);

                // GBP/PLN
                var gbpRate = await _api.GetGbpToPlnFromNbpAsync();
                _logger.LogInformation("NBP GBP/PLN: {Rate}", gbpRate);

                // Create scope for scoped service
                using (var scope = _serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IExchangeRateService>();
                    await service.SaveUsdRateAsync(usdRate);
                    await service.SaveEurRateAsync(eurRate);
                    await service.SaveGbpRateAsync(gbpRate);
                }
                
                _logger.LogInformation("Exchange rates saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR fetching rates: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}