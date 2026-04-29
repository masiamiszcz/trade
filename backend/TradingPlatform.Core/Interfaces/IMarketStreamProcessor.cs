using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface IMarketStreamProcessor
{
    Task ProcessMessageAsync(BinanceStreamMessage message, CancellationToken cancellationToken = default);
    Task<MarketTickDto?> NormalizeAsync(BinanceStreamMessage message, CancellationToken cancellationToken = default);
    Task PublishPriceAsync(MarketTickDto tick, CancellationToken cancellationToken = default);
}
