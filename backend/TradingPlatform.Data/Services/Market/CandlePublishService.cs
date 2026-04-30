using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services.Market;

public sealed class CandlePublishService : BackgroundService
{
    private readonly ILogger<CandlePublishService> _logger;
    private readonly ICandleRepository _candleRepository;
    private readonly IPriceUpdatePublisher _publisher;

    public CandlePublishService(
        ILogger<CandlePublishService> logger,
        ICandleRepository candleRepository,
        IPriceUpdatePublisher publisher)
    {
        _logger = logger;
        _candleRepository = candleRepository;
        _publisher = publisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕒 Candle Publish Service started, waiting for interval boundaries...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var boundary = GetNextMinuteBoundary(now);
            var delay = boundary - now + TimeSpan.FromSeconds(2);

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await PublishBoundaryCandlesAsync(boundary, stoppingToken);
        }
    }

    private async Task PublishBoundaryCandlesAsync(DateTime publishTime, CancellationToken cancellationToken)
    {
        await PublishIntervalCandlesAsync(1, publishTime, cancellationToken);

        if (publishTime.Minute % 5 == 0)
        {
            await PublishIntervalCandlesAsync(5, publishTime, cancellationToken);
        }

        if (publishTime.Minute == 0)
        {
            await PublishIntervalCandlesAsync(60, publishTime, cancellationToken);
        }
    }

    private async Task PublishIntervalCandlesAsync(int intervalMinutes, DateTime publishTime, CancellationToken cancellationToken)
    {
        var candleOpenTime = publishTime.AddMinutes(-intervalMinutes);
        var candles = await _candleRepository.GetBySourceIntervalOpenTimeAsync(
            source: "binance",
            intervalMinutes: intervalMinutes,
            openTime: candleOpenTime,
            cancellationToken: cancellationToken);

        if (!candles.Any())
        {
            _logger.LogDebug("No persisted {Interval}m candles found for open time {OpenTime:O}", intervalMinutes, candleOpenTime);
            return;
        }

        foreach (var candle in candles)
        {
            var candleDto = new CandleDto(
                candle.Symbol,
                candle.OpenTime,
                candle.CloseTime,
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close,
                candle.Volume,
                GetIntervalKey(intervalMinutes));

            try
            {
                await _publisher.PublishCandleUpdateAsync(candleDto, GetIntervalKey(intervalMinutes), cancellationToken);
                _logger.LogInformation(
                    "📣 Published closed {Interval}m candle for {Symbol} at {OpenTime:O}",
                    intervalMinutes,
                    candle.Symbol,
                    candle.OpenTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to publish {Interval}m candle for {Symbol}", intervalMinutes, candle.Symbol);
            }
        }
    }

    private static string GetIntervalKey(int intervalMinutes)
        => intervalMinutes switch
        {
            1 => "1m",
            5 => "5m",
            60 => "1h",
            _ => $"{intervalMinutes}m",
        };

    private static DateTime GetNextMinuteBoundary(DateTime utcNow)
    {
        var next = utcNow.AddMinutes(1);
        return new DateTime(next.Year, next.Month, next.Day, next.Hour, next.Minute, 0, DateTimeKind.Utc);
    }
}
