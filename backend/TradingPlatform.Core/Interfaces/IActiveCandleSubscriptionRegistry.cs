using System.Collections.Generic;

namespace TradingPlatform.Core.Interfaces;

public interface IActiveCandleSubscriptionRegistry
{
    bool TryAddActiveInterval(string symbol, int intervalMinutes);
    bool TryRemoveActiveInterval(string symbol, int intervalMinutes);
    IReadOnlyCollection<int> GetActiveIntervals(string symbol);
}
