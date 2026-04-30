using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Core.Interfaces;

public interface ICandleStreamActivationService
{
    Task InitializeActiveIntervalAsync(string symbol, int intervalMinutes, CancellationToken cancellationToken = default);
}
