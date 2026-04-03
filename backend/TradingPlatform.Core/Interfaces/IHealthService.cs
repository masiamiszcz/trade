using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IHealthService
{
    Task<HealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}