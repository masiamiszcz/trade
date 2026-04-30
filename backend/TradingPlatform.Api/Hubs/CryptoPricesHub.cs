using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Hubs;

public class CryptoPricesHub : Hub
{
    private readonly IActiveCandleSubscriptionRegistry _subscriptionRegistry;
    private readonly ICandleStreamActivationService _streamActivationService;

    private sealed record SubscriptionState(string Symbol, int RangeMinutes, int IntervalMinutes, DateTime StartTime);

    private static readonly ConcurrentDictionary<string, SubscriptionState> ConnectionSubscriptions = new();

    public CryptoPricesHub(
        IActiveCandleSubscriptionRegistry subscriptionRegistry,
        ICandleStreamActivationService streamActivationService)
    {
        _subscriptionRegistry = subscriptionRegistry;
        _streamActivationService = streamActivationService;
    }

    public async Task SubscribeToSymbol(string symbol, int rangeMinutes)
    {
        if (rangeMinutes <= 0)
        {
            throw new HubException("RangeMinutes must be greater than zero.");
        }

        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var intervalMinutes = ResolveIntervalMinutes(rangeMinutes);
        var symbolGroup = GetSymbolGroup(normalizedSymbol);
        var candleGroup = GetCandleGroup(symbolGroup, GetIntervalKey(intervalMinutes));

        await CleanupPreviousSubscriptionAsync();

        await Groups.AddToGroupAsync(Context.ConnectionId, symbolGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, candleGroup);

        ConnectionSubscriptions[Context.ConnectionId] = new SubscriptionState(
            normalizedSymbol,
            rangeMinutes,
            intervalMinutes,
            DateTime.UtcNow);

        if (_subscriptionRegistry.TryAddActiveInterval(normalizedSymbol, intervalMinutes))
        {
            await _streamActivationService.InitializeActiveIntervalAsync(normalizedSymbol, intervalMinutes);
        }
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        await CleanupPreviousSubscriptionAsync(normalizedSymbol);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await CleanupPreviousSubscriptionAsync();
        await base.OnDisconnectedAsync(exception);
    }

    public async Task BroadcastPriceUpdate(PriceUpdateDto update)
    {
        await Clients.Group(GetSymbolGroup(update.Symbol)).SendAsync("ReceiveMarketUpdate", new MarketStreamUpdateDto(update, null));
    }

    private async Task CleanupPreviousSubscriptionAsync(string? fallbackSymbol = null)
    {
        if (ConnectionSubscriptions.TryRemove(Context.ConnectionId, out var subscription))
        {
            await Task.WhenAll(
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSymbolGroup(subscription.Symbol)),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(GetSymbolGroup(subscription.Symbol), GetIntervalKey(subscription.IntervalMinutes))));

            _subscriptionRegistry.TryRemoveActiveInterval(subscription.Symbol, subscription.IntervalMinutes);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackSymbol))
        {
            var symbolGroup = GetSymbolGroup(fallbackSymbol);
            await Task.WhenAll(
                Groups.RemoveFromGroupAsync(Context.ConnectionId, symbolGroup),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, "1m")),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, "5m")),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, "15m")),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, "30m")),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, "1h")),
                Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, "1d")));
        }
    }

    private static int ResolveIntervalMinutes(int rangeMinutes)
    {
        const int DAY = 1440;
        const int YEAR = 525600;

        if (rangeMinutes <= DAY)
            return 1;

        if (rangeMinutes <= 7 * DAY)
            return 5;

        if (rangeMinutes <= 14 * DAY)
            return 15;

        if (rangeMinutes <= 30 * DAY)
            return 30;

        if (rangeMinutes <= YEAR)
            return 60;

        return 1440;
    }

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

    private static string GetSymbolGroup(string symbol)
        => symbol.Trim().ToUpperInvariant();

    private static string GetCandleGroup(string symbol, string interval)
        => $"{symbol}:{interval.Trim().ToLowerInvariant()}";
}
