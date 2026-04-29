using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface IPriceUpdatePublisher
{
    Task PublishAsync(PriceUpdateDto priceUpdate, CancellationToken cancellationToken = default);
    Task PublishBatchAsync(IEnumerable<PriceUpdateDto> priceUpdates, CancellationToken cancellationToken = default);
}
