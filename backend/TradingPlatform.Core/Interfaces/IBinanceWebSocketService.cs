using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface IBinanceWebSocketService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }
    Task<decimal?> GetLatestPriceAsync(string symbol, CancellationToken cancellationToken = default);
    Task PublishMarketStateAsync(MarketStateDto marketState, CancellationToken cancellationToken = default);
}
