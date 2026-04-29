using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TradingPlatform.Api.Hubs;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Api.Services;

public class PriceUpdatePublisher : IPriceUpdatePublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<CryptoPricesHub> _hubContext;
    private readonly ILogger<PriceUpdatePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeSpan _cacheTtl;

    public PriceUpdatePublisher(
        IConnectionMultiplexer redis,
        IHubContext<CryptoPricesHub> hubContext,
        ILogger<PriceUpdatePublisher> logger,
        IOptions<BinanceSettings> binanceSettings)
    {
        _redis = redis;
        _hubContext = hubContext;
        _logger = logger;

        var ttlSeconds = Math.Max(1, binanceSettings.Value.PriceCacheTtlSeconds);
        _cacheTtl = TimeSpan.FromSeconds(ttlSeconds);
    }

    public async Task PublishAsync(PriceUpdateDto priceUpdate, CancellationToken cancellationToken = default)
    {
        var symbolKey = priceUpdate.Symbol.ToUpperInvariant();
        var redisKey = $"market:latest:{symbolKey}";
        var payload = JsonSerializer.Serialize(priceUpdate, _jsonOptions);

        try
        {
            var db = _redis.GetDatabase();
            await db.StringSetAsync(redisKey, payload, _cacheTtl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis publish failed for {Symbol}", symbolKey);
        }

        try
        {
            await _hubContext.Clients.Group(symbolKey)
                .SendAsync("ReceivePriceUpdate", priceUpdate, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR publish failed for {Symbol}", symbolKey);
        }
    }

    public async Task PublishBatchAsync(IEnumerable<PriceUpdateDto> priceUpdates, CancellationToken cancellationToken = default)
    {
        foreach (var update in priceUpdates)
        {
            await PublishAsync(update, cancellationToken);
        }
    }
}
