using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Context;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlAssetMappingRepository(IServiceProvider serviceProvider) : IAssetMappingRepository
{
    public async Task<string?> GetCoingeckoIdBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        return await dbContext.AssetMappings
            .Where(a => a.Symbol == symbol && a.Source == "coingecko")
            .Select(a => a.CoingeckoId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
