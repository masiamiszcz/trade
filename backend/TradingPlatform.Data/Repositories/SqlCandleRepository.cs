using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public class SqlCandleRepository(IServiceProvider serviceProvider) : ICandleRepository
{
    public async Task AddAsync(CandleEntity candle, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();
        
        await dbContext.Candles.AddAsync(candle, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<CandleEntity>> GetBySymbolAsync(string symbol, int limit = 100, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();
        
        return await dbContext.Candles
            .Where(c => c.Symbol == symbol)
            .OrderByDescending(c => c.OpenTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
