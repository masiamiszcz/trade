using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services.Market;

public class MarketProcessingService : BackgroundService, IMarketDataHandler
{
    private readonly Channel<Trade> _channel;
    private readonly ILogger<MarketProcessingService> _logger;
    private readonly ICandleRepository _candleRepository;
    private readonly IPriceUpdatePublisher _publisher;
    
    // Candle aggregation state
    private readonly Dictionary<string, Candle> _currentCandles = new();
    private DateTime _currentMinuteBoundary = GetMinuteBoundary(DateTime.UtcNow);

    public MarketProcessingService(
        Channel<Trade> channel, 
        ILogger<MarketProcessingService> logger,
        ICandleRepository candleRepository,
        IPriceUpdatePublisher publisher)
    {
        _channel = channel;
        _logger = logger;
        _candleRepository = candleRepository;
        _publisher = publisher;
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
        
        // Nowa minuta - zapisz starą candle do DB
        if (tradeMinuteBoundary > _currentMinuteBoundary)
        {
            if (_currentCandles.TryGetValue(trade.Symbol, out var completedCandle))
            {
                await SaveCandleAsync(completedCandle);
            }
            _currentMinuteBoundary = tradeMinuteBoundary;
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
    }

    private async Task SaveCandleAsync(Candle candle)
    {
        try
        {
            var entity = new CandleEntity
            {
                Symbol = candle.Symbol,
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

    private void LogCandle(Candle candle)
    {
        _logger.LogInformation(
            "📊 CANDLE: {Symbol} {Time:yyyy-MM-dd HH:mm} O:{Open} H:{High} L:{Low} C:{Close} Vol:{Volume:F2}",
            candle.Symbol,
            candle.OpenTime,
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.Volume
        );
    }

    private static DateTime GetMinuteBoundary(DateTime dateTime)
    {
        return dateTime.AddSeconds(-dateTime.Second).AddMilliseconds(-dateTime.Millisecond);
    }
}