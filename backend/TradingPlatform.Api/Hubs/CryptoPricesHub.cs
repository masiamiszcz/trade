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

    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SubscriptionState>> ConnectionSubscriptions = new();

    public CryptoPricesHub(
        IActiveCandleSubscriptionRegistry subscriptionRegistry,
        ICandleStreamActivationService streamActivationService)
    {
        _subscriptionRegistry = subscriptionRegistry;
        _streamActivationService = streamActivationService;
    }

    public async Task SubscribeToSymbol(string symbol, int rangeMinutes)
        => await SubscribeToSymbols(new[] { symbol }, rangeMinutes);

    public async Task SubscribeToSymbols(string[] symbols, int rangeMinutes)
    {
        if (rangeMinutes <= 0)
        {
            throw new HubException("RangeMinutes must be greater than zero.");
        }
        if (symbols is null)
        {
            throw new HubException("At least one symbol must be provided.");
        }

        var normalizedSymbols = symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedSymbols.Length == 0 || normalizedSymbols.Length > 4)
        {
            throw new HubException("Between one and four symbols must be provided.");
        }

        var intervalMinutes = ResolveIntervalMinutes(rangeMinutes);
        await CleanupPreviousSubscriptionAsync();

        var subscriptions = ConnectionSubscriptions.GetOrAdd(
            Context.ConnectionId,
            _ => new ConcurrentDictionary<string, SubscriptionState>(StringComparer.OrdinalIgnoreCase));
        try
        {
            foreach (var normalizedSymbol in normalizedSymbols)
            {
                var symbolGroup = GetSymbolGroup(normalizedSymbol);
                var candleGroup = GetCandleGroup(symbolGroup, GetIntervalKey(intervalMinutes));

                subscriptions[normalizedSymbol] = new SubscriptionState(
                    normalizedSymbol,
                    rangeMinutes,
                    intervalMinutes,
                    DateTime.UtcNow);
                await Groups.AddToGroupAsync(Context.ConnectionId, symbolGroup);
                await Groups.AddToGroupAsync(Context.ConnectionId, candleGroup);

                if (_subscriptionRegistry.TryAddActiveInterval(normalizedSymbol, intervalMinutes))
                {
                    await _streamActivationService.InitializeActiveIntervalAsync(normalizedSymbol, intervalMinutes);
                }
            }
        }
        catch
        {
            await CleanupPreviousSubscriptionAsync();
            throw;
        }
    }

    public async Task UnsubscribeFromSymbol(string symbol)
        => await UnsubscribeFromSymbols(new[] { symbol });

    public async Task UnsubscribeFromSymbols(string[] symbols)
    {
        if (symbols is null)
        {
            return;
        }

        var normalizedSymbols = symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        await CleanupPreviousSubscriptionAsync(normalizedSymbols);
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

    private async Task CleanupPreviousSubscriptionAsync(IReadOnlyCollection<string>? symbolsToRemove = null)
    {
        if (!ConnectionSubscriptions.TryGetValue(Context.ConnectionId, out var subscriptions))
        {
            if (symbolsToRemove is not null)
            {
                await RemoveFallbackGroupsAsync(symbolsToRemove);
            }
            return;
        }

        var symbols = symbolsToRemove is null
            ? subscriptions.Keys.ToArray()
            : symbolsToRemove;
        foreach (var symbol in symbols)
        {
            if (subscriptions.TryRemove(symbol, out var subscription))
            {
                var symbolGroup = GetSymbolGroup(subscription.Symbol);
                await Task.WhenAll(
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, symbolGroup),
                    Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCandleGroup(symbolGroup, GetIntervalKey(subscription.IntervalMinutes))));
                _subscriptionRegistry.TryRemoveActiveInterval(subscription.Symbol, subscription.IntervalMinutes);
            }
            else if (symbolsToRemove is not null)
            {
                await RemoveFallbackGroupsAsync(new[] { symbol });
            }
        }

        if (subscriptions.IsEmpty)
        {
            ConnectionSubscriptions.TryRemove(Context.ConnectionId, out _);
        }
    }

    private async Task RemoveFallbackGroupsAsync(IEnumerable<string> symbols)
    {
        foreach (var symbol in symbols)
        {
            var symbolGroup = GetSymbolGroup(symbol);
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
