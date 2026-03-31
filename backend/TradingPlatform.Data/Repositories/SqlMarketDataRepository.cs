using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlMarketDataRepository(TradingPlatformDbContext dbContext) : IMarketDataRepository
{
    public async Task<IReadOnlyList<MarketAsset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.MarketAssets
            .AsNoTracking()
            .OrderBy(x => x.Symbol)
            .Select(x => new MarketAsset(x.Symbol, x.Name, x.Price, x.Currency, x.ChangePercent))
            .ToListAsync(cancellationToken);
    }

    public async Task<MarketAsset?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        return await dbContext.MarketAssets
            .AsNoTracking()
            .Where(x => x.Symbol == symbol)
            .Select(x => new MarketAsset(x.Symbol, x.Name, x.Price, x.Currency, x.ChangePercent))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
