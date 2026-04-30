using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.External;

namespace TradingPlatform.Data.Services.Market;

public sealed class CoingeckoStartupPollingService : BackgroundService
{
    private readonly ICoingeckoApiClient _apiClient;
    private readonly CoingeckoSettings _settings;
    private readonly ILogger<CoingeckoStartupPollingService> _logger;

    public CoingeckoStartupPollingService(
        ICoingeckoApiClient apiClient,
        IOptions<CoingeckoSettings> options,
        ILogger<CoingeckoStartupPollingService> logger)
    {
        _apiClient = apiClient;
        _settings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Startup Coingecko polling service initialising...");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        try
        {
            _logger.LogInformation("Polling Coingecko market chart for {AssetId} vs {Currency}...", _settings.AssetId, _settings.VsCurrency);
            var chart = await _apiClient.GetMarketChartAsync(
                _settings.AssetId,
                _settings.VsCurrency,
                _settings.MarketChartDays,
                _settings.Interval,
                stoppingToken);

            _logger.LogInformation(
                "Coingecko startup polling complete. Prices: {PriceCount}, MarketCaps: {MarketCapCount}, Volumes: {VolumeCount}.",
                chart.Prices.Count,
                chart.MarketCaps.Count,
                chart.TotalVolumes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coingecko startup polling failed: {Message}", ex.Message);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
