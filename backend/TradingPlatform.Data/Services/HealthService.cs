using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Data.Services;

public class HealthService : IHealthService
{
    private readonly TradingPlatformDbContext _context;

    public HealthService(TradingPlatformDbContext context)
    {
        _context = context;
    }

    public async Task<HealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var isDatabaseHealthy = await _context.Database.CanConnectAsync(cancellationToken);

        return new HealthStatus
        {
            Status = isDatabaseHealthy ? "Healthy" : "Unhealthy",
            IsReady = isDatabaseHealthy,
            DatabaseHealthy = isDatabaseHealthy,
            Message = isDatabaseHealthy
                ? "Application is running and database is accessible."
                : "Database connection failed.",
            Timestamp = DateTime.UtcNow
        };
    }
}