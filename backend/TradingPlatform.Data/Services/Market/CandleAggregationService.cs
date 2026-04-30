using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services.Market;

public sealed class CandleAggregationService
{
    private readonly ICandleRepository _candleRepository;
    private readonly ILogger<CandleAggregationService> _logger;
    private readonly Dictionary<string, Candle> _current5mCandles = new();
    private readonly Dictionary<string, Candle> _current1hCandles = new();

    public CandleAggregationService(
        ICandleRepository candleRepository,
        ILogger<CandleAggregationService> logger)
    {
        _candleRepository = candleRepository;
        _logger = logger;
    }

    public async Task HandleCompletedCandleAsync(Candle completedMinuteCandle, CancellationToken cancellationToken = default)
    {
        // Aggregation-only: do not publish candles here.
        // This service persists 5m/1h candles after 1m close,
        // while publishing is handled by the dedicated publisher flow.
        await AggregateIntervalAsync(completedMinuteCandle, 5, _current5mCandles, cancellationToken);
        await AggregateIntervalAsync(completedMinuteCandle, 60, _current1hCandles, cancellationToken);
    }

    private async Task AggregateIntervalAsync(
        Candle completedMinuteCandle,
        int intervalMinutes,
        Dictionary<string, Candle> currentCandles,
        CancellationToken cancellationToken)
    {
        var intervalStart = GetIntervalBoundary(completedMinuteCandle.OpenTime, intervalMinutes);
        var key = GetKey(completedMinuteCandle.Symbol, intervalMinutes);

        if (!currentCandles.TryGetValue(key, out var aggregateCandle))
        {
            currentCandles[key] = CreateAggregateCandle(completedMinuteCandle, intervalMinutes, intervalStart);
            return;
        }

        if (aggregateCandle.OpenTime != intervalStart)
        {
            await SaveAggregateCandleAsync(aggregateCandle, intervalMinutes, cancellationToken);
            currentCandles[key] = CreateAggregateCandle(completedMinuteCandle, intervalMinutes, intervalStart);
            return;
        }

        UpdateAggregateCandle(aggregateCandle, completedMinuteCandle);
    }

    private static Candle CreateAggregateCandle(Candle minuteCandle, int intervalMinutes, DateTime intervalStart)
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

    private async Task SaveAggregateCandleAsync(Candle candle, int intervalMinutes, CancellationToken cancellationToken)
    {
        try
        {
            var entity = new CandleEntity
            {
                Symbol = candle.Symbol,
                Source = "binance",
                IntervalMinutes = intervalMinutes,
                OpenTime = candle.OpenTime,
                CloseTime = candle.CloseTime,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume
            };

            await _candleRepository.AddAsync(entity, cancellationToken);

            _logger.LogInformation(
                "💾 AGGREGATED {Interval}m: {Symbol} {Time:yyyy-MM-dd HH:mm} O:{Open} H:{High} L:{Low} C:{Close} Vol:{Volume:F2}",
                intervalMinutes,
                candle.Symbol,
                candle.OpenTime,
                candle.Open,
                candle.High,
                candle.Low,
                candle.Close,
                candle.Volume);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error saving aggregated {Interval}m candle for {Symbol}", intervalMinutes, candle.Symbol);
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

    private static string GetKey(string symbol, int intervalMinutes)
        => $"{symbol}:{intervalMinutes}";
}
