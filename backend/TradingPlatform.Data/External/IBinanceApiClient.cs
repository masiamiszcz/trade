using System.Threading;
using System.Threading.Tasks;

namespace TradingPlatform.Data.External;

public interface IBinanceApiClient
{
    Task<IEnumerable<BinanceKline>> GetHistoricalKlinesAsync(
        string symbol,
        string interval,
        int limit,
        DateTime? startTime = null,
        CancellationToken cancellationToken = default);
}
