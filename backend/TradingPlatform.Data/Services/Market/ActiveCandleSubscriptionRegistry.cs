using System.Collections.Concurrent;
using System.Linq;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Data.Services.Market;

public sealed class ActiveCandleSubscriptionRegistry : IActiveCandleSubscriptionRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, int>> _activeIntervals
        = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAddActiveInterval(string symbol, int intervalMinutes)
    {
        var intervals = _activeIntervals.GetOrAdd(symbol.Trim().ToUpperInvariant(), _ => new ConcurrentDictionary<int, int>());
        var count = intervals.AddOrUpdate(intervalMinutes, 1, (_, existing) => existing + 1);
        return count == 1;
    }

    public bool TryRemoveActiveInterval(string symbol, int intervalMinutes)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        if (!_activeIntervals.TryGetValue(normalizedSymbol, out var intervals))
        {
            return false;
        }

        if (!intervals.TryGetValue(intervalMinutes, out var existingCount))
        {
            return false;
        }

        if (existingCount <= 1)
        {
            intervals.TryRemove(intervalMinutes, out _);
        }
        else
        {
            intervals.TryUpdate(intervalMinutes, existingCount - 1, existingCount);
        }

        if (intervals.IsEmpty)
        {
            _activeIntervals.TryRemove(normalizedSymbol, out _);
        }

        return existingCount == 1;
    }

    public IReadOnlyCollection<int> GetActiveIntervals(string symbol)
    {
        if (_activeIntervals.TryGetValue(symbol.Trim().ToUpperInvariant(), out var intervals))
        {
            return intervals.Keys.ToArray();
        }

        return Array.Empty<int>();
    }
}
