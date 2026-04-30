using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services.Market;

public class MarketProcessingService : BackgroundService, IMarketDataHandler, ICandleStreamActivationService
{
    private readonly Channel<Trade> _channel;
    private readonly ILogger<MarketProcessingService> _logger;
    private readonly ICandleRepository _candleRepository;
    private readonly IPriceUpdatePublisher _publisher;
    private readonly IActiveCandleSubscriptionRegistry _activeSubscriptionRegistry;
    
    // Candle aggregation state
    private readonly Dictionary<string, Candle> _currentCandles = new();
    private readonly Dictionary<string, DateTime> _currentMinuteBoundaries = new();
    private readonly Dictionary<string, Candle> _streamAggregateCandles = new();

    private static readonly int[] StreamingIntervals = new[] { 1, 5, 15, 30, 60, 1440 };

    public MarketProcessingService(
        Channel<Trade> channel, 
        ILogger<MarketProcessingService> logger,
        ICandleRepository candleRepository,
        IPriceUpdatePublisher publisher,
        IActiveCandleSubscriptionRegistry activeSubscriptionRegistry)
    {
        _channel = channel;
        _logger = logger;
        _candleRepository = candleRepository;
        _publisher = publisher;
        _activeSubscriptionRegistry = activeSubscriptionRegistry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("📊 Market Processing Service started, listening for trades...");

        await foreach (var trade in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessTradeAsync(trade);
        }
    }

    /// <summary>
    /// Handles trade directly from BinanceWebSocketService (legacy path, kept for interface compliance)
    /// Real processing happens in ProcessTrade via Channel
    /// </summary>
    public async Task HandleAsync(Trade trade)
    {
        await Task.CompletedTask;
    }

    private async Task ProcessTradeAsync(Trade trade)
    {
        var tickUpdate = new PriceUpdateDto
        {
            Symbol = trade.Symbol,
            Price = trade.Price,
            Volume = trade.Quantity,
            Timestamp = trade.Timestamp
        };

        try
        {
            await _publisher.PublishAsync(tickUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error publishing tick for {Symbol}", trade.Symbol);
        }

        var tradeMinuteBoundary = GetMinuteBoundary(trade.Timestamp);
        if (!_currentMinuteBoundaries.TryGetValue(trade.Symbol, out var currentMinuteBoundary))
        {
            currentMinuteBoundary = tradeMinuteBoundary;
            _currentMinuteBoundaries[trade.Symbol] = tradeMinuteBoundary;
        }

        // Nowa minuta dla symbolu - zapisz starą candle do DB i opublikuj candle updates
        if (tradeMinuteBoundary > currentMinuteBoundary)
        {
            if (_currentCandles.TryGetValue(trade.Symbol, out var completedCandle))
            {
                await PublishClosedCandlesAsync(completedCandle);
                await SaveCandleAsync(completedCandle);
                _currentCandles.Remove(trade.Symbol);
            }

            _currentMinuteBoundaries[trade.Symbol] = tradeMinuteBoundary;
        }

        // Aktualizuj/utwórz candle dla obecnej minuty
        if (!_currentCandles.TryGetValue(trade.Symbol, out var candle))
        {
            candle = new Candle
            {
                Symbol = trade.Symbol,
                OpenTime = tradeMinuteBoundary,
                CloseTime = tradeMinuteBoundary.AddMinutes(1),
                Open = trade.Price,
                High = trade.Price,
                Low = trade.Price,
                Close = trade.Price,
                Volume = 0
            };
            _currentCandles[trade.Symbol] = candle;
        }

        // Aktualizuj OHLC
        candle.High = Math.Max(candle.High, trade.Price);
        candle.Low = Math.Min(candle.Low, trade.Price);
        candle.Close = trade.Price;
        candle.Volume += trade.Quantity;

        // Do not publish partial 1m candles before they close.
        // Chart updates should be emitted only when the candle is completed.
    }

    private async Task SaveCandleAsync(Candle candle)
    {
        try
        {
            var entity = new CandleEntity
            {
                Symbol = candle.Symbol,
                Source = "binance",
                IntervalMinutes = 1,
                OpenTime = candle.OpenTime,
                CloseTime = candle.CloseTime,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            };

            await _candleRepository.AddAsync(entity);
            
            _logger.LogInformation(
                "💾 SAVED: {Symbol} {Time:yyyy-MM-dd HH:mm} O:{Open} H:{High} L:{Low} C:{Close} Vol:{Volume:F2}",
                candle.Symbol,
                candle.OpenTime,
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close,
                candle.Volume
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error saving candle {Symbol}", candle.Symbol);
        }
    }

    private async Task PublishClosedCandlesAsync(Candle completedMinuteCandle)
    {
        var activeIntervals = _activeSubscriptionRegistry.GetActiveIntervals(completedMinuteCandle.Symbol);
        if (activeIntervals.Count == 0)
        {
            return;
        }

        if (activeIntervals.Contains(1))
        {
            await PublishStreamCandleAsync(completedMinuteCandle, 1);
        }

        foreach (var intervalMinutes in activeIntervals.Where(i => i != 1))
        {
            await UpdateStreamAggregateAsync(completedMinuteCandle, intervalMinutes);
        }
    }

    private async Task PublishCurrentStreamCandleStateAsync(Candle currentMinuteCandle)
    {
        var activeIntervals = _activeSubscriptionRegistry.GetActiveIntervals(currentMinuteCandle.Symbol);
        if (activeIntervals.Count == 0)
        {
            return;
        }

        if (activeIntervals.Contains(1))
        {
            await PublishStreamCandleAsync(currentMinuteCandle, 1);
        }

        // For aggregated intervals larger than 1 minute, do not publish partial interval candles.
        // This avoids an immediate first update on chart load and ensures the first update
        // is published only when the completed interval is available.
    }

    public async Task InitializeActiveIntervalAsync(string symbol, int intervalMinutes, CancellationToken cancellationToken = default)
    {
        if (intervalMinutes == 1)
        {
            return;
        }

        var intervalStart = GetIntervalBoundary(DateTime.UtcNow, intervalMinutes);
        var key = GetStreamKey(symbol, intervalMinutes);

        if (_streamAggregateCandles.TryGetValue(key, out var existing) && existing.OpenTime == intervalStart)
        {
            return;
        }

        var currentMinuteBoundary = GetMinuteBoundary(DateTime.UtcNow);
        var completedMinutes = await _candleRepository.GetBySymbolSourceIntervalAsync(
            symbol,
            "binance",
            1,
            intervalStart,
            currentMinuteBoundary,
            cancellationToken);

        if (!completedMinutes.Any())
        {
            return;
        }

        _streamAggregateCandles[key] = CreateStreamAggregateFromCompletedMinutes(
            symbol,
            intervalMinutes,
            intervalStart,
            completedMinutes);
    }

    private Candle CreateStreamAggregateFromCompletedMinutes(
        string symbol,
        int intervalMinutes,
        DateTime intervalStart,
        List<CandleEntity> completedMinutes)
    {
        var orderedMinutes = completedMinutes.OrderBy(c => c.OpenTime).ToList();
        return new Candle
        {
            Symbol = symbol,
            OpenTime = intervalStart,
            CloseTime = intervalStart.AddMinutes(intervalMinutes),
            Open = orderedMinutes.First().Open,
            High = orderedMinutes.Max(c => c.High),
            Low = orderedMinutes.Min(c => c.Low),
            Close = orderedMinutes.Last().Close,
            Volume = orderedMinutes.Sum(c => c.Volume)
        };
    }

    private Candle BuildCurrentIntervalCandle(Candle currentMinuteCandle, int intervalMinutes)
    {
        var intervalStart = GetIntervalBoundary(currentMinuteCandle.OpenTime, intervalMinutes);
        var key = GetStreamKey(currentMinuteCandle.Symbol, intervalMinutes);

        if (!_streamAggregateCandles.TryGetValue(key, out var baseAggregate)
            || baseAggregate.OpenTime != intervalStart)
        {
            return CreateStreamAggregateCandle(currentMinuteCandle, intervalMinutes, intervalStart);
        }

        return CombineCurrentIntervalCandle(baseAggregate, currentMinuteCandle, intervalStart, intervalMinutes);
    }

    private Candle CombineCurrentIntervalCandle(Candle baseAggregate, Candle currentMinuteCandle, DateTime intervalStart, int intervalMinutes)
    {
        return new Candle
        {
            Symbol = baseAggregate.Symbol,
            OpenTime = intervalStart,
            CloseTime = intervalStart.AddMinutes(intervalMinutes),
            Open = baseAggregate.Open,
            High = Math.Max(baseAggregate.High, currentMinuteCandle.High),
            Low = Math.Min(baseAggregate.Low, currentMinuteCandle.Low),
            Close = currentMinuteCandle.Close,
            Volume = baseAggregate.Volume + currentMinuteCandle.Volume
        };
    }

    private async Task UpdateStreamAggregateAsync(Candle completedMinuteCandle, int intervalMinutes)
    {
        var key = GetStreamKey(completedMinuteCandle.Symbol, intervalMinutes);
        var intervalStart = GetIntervalBoundary(completedMinuteCandle.OpenTime, intervalMinutes);

        if (!_streamAggregateCandles.TryGetValue(key, out var aggregateCandle))
        {
            _streamAggregateCandles[key] = CreateStreamAggregateCandle(completedMinuteCandle, intervalMinutes, intervalStart);
            return;
        }

        if (aggregateCandle.OpenTime != intervalStart)
        {
            await PublishStreamCandleAsync(aggregateCandle, intervalMinutes);
            _streamAggregateCandles[key] = CreateStreamAggregateCandle(completedMinuteCandle, intervalMinutes, intervalStart);
            return;
        }

        UpdateAggregateCandle(aggregateCandle, completedMinuteCandle);
    }

    private Candle CreateStreamAggregateCandle(Candle minuteCandle, int intervalMinutes, DateTime intervalStart)
    {
        return new Candle
        {
            Symbol = minuteCandle.Symbol,
            OpenTime = intervalStart,
            CloseTime = intervalStart.AddMinutes(intervalMinutes),
            Open = minuteCandle.Open,
            High = minuteCandle.High,
            Low = minuteCandle.Low,
            Close = minuteCandle.Close,
            Volume = minuteCandle.Volume
        };
    }

    private static void UpdateAggregateCandle(Candle aggregate, Candle minuteCandle)
    {
        aggregate.High = Math.Max(aggregate.High, minuteCandle.High);
        aggregate.Low = Math.Min(aggregate.Low, minuteCandle.Low);
        aggregate.Close = minuteCandle.Close;
        aggregate.Volume += minuteCandle.Volume;
    }

    private async Task PublishStreamCandleAsync(Candle candle, int intervalMinutes)
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
            await _publisher.PublishCandleUpdateAsync(candleDto, GetIntervalKey(intervalMinutes));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error publishing stream candle {Interval}m for {Symbol}", intervalMinutes, candle.Symbol);
        }
    }

    private static DateTime GetIntervalBoundary(DateTime dateTime, int intervalMinutes)
    {
        var totalMinutes = dateTime.Hour * 60 + dateTime.Minute;
        var boundaryMinutes = totalMinutes / intervalMinutes * intervalMinutes;
        var hours = boundaryMinutes / 60;
        var minutes = boundaryMinutes % 60;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hours, minutes, 0, DateTimeKind.Utc);
    }

    private static string GetStreamKey(string symbol, int intervalMinutes)
        => $"{symbol}:{intervalMinutes}";

    private static string GetIntervalKey(int intervalMinutes)
        => intervalMinutes switch
        {
            1 => "1m",
            5 => "5m",
            15 => "15m",
            30 => "30m",
            60 => "1h",
            1440 => "1d",
            _ => $"{intervalMinutes}m",
        };

    private static DateTime GetMinuteBoundary(DateTime dateTime)
    {
        return dateTime.AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
    }
}