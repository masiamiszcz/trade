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

    public async Task<List<CandleEntity>> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();
        
        return await dbContext.Candles
            .Where(c => c.Symbol == symbol)
            .OrderByDescending(c => c.OpenTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CandleEntity>> GetBySymbolSourceIntervalAsync(
        string symbol,
        string source,
        int intervalMinutes,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        return await dbContext.Candles
            .Where(c => c.Symbol == symbol && c.Source == source && c.IntervalMinutes == intervalMinutes)
            .Where(c => c.OpenTime >= from && c.OpenTime < to)
            .OrderByDescending(c => c.OpenTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CandleEntity>> GetAggregatedFromOneMinuteSourceAsync(
        string symbol,
        string source,
        int targetIntervalMinutes,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var query = dbContext.Candles
            .Where(c => c.Symbol == symbol && c.Source == source && c.IntervalMinutes == 1)
            .Where(c => c.OpenTime >= from && c.OpenTime < to)
            .GroupBy(c => EF.Functions.DateDiffMinute(epoch, c.OpenTime) / targetIntervalMinutes)
            .Select(g => new
            {
                BucketKey = g.Key,
                OpenCandle = g.OrderBy(c => c.OpenTime).First(),
                CloseCandle = g.OrderByDescending(c => c.OpenTime).First(),
                High = g.Max(c => c.High),
                Low = g.Min(c => c.Low),
                Volume = g.Sum(c => c.Volume)
            })
            .Select(bucket => new CandleEntity
            {
                Symbol = symbol,
                Source = source,
                IntervalMinutes = targetIntervalMinutes,
                OpenTime = epoch.AddMinutes(bucket.BucketKey * targetIntervalMinutes),
                CloseTime = epoch.AddMinutes((bucket.BucketKey + 1) * targetIntervalMinutes),
                Open = bucket.OpenCandle.Open,
                High = bucket.High,
                Low = bucket.Low,
                Close = bucket.CloseCandle.Close,
                Volume = bucket.Volume
            })
            .OrderBy(c => c.OpenTime);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<CandleEntity>> GetBySourceIntervalOpenTimeAsync(
        string source,
        int intervalMinutes,
        DateTime openTime,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        return await dbContext.Candles
            .Where(c => c.Source == source && c.IntervalMinutes == intervalMinutes && c.OpenTime == openTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string source, int intervalMinutes, DateTime openTime, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        return await dbContext.Candles
            .AnyAsync(c => c.Source == source && c.IntervalMinutes == intervalMinutes && c.OpenTime == openTime, cancellationToken);
    }

    public async Task<DateTime?> GetLastCandleTimestampAsync(string symbol, string source, int intervalMinutes, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        return await dbContext.Candles
            .Where(c => c.Symbol == symbol && c.Source == source && c.IntervalMinutes == intervalMinutes)
            .OrderByDescending(c => c.OpenTime)
            .Select(c => (DateTime?)c.OpenTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DateTime>> GetExistingOpenTimesAsync(
        string symbol,
        string source,
        int intervalMinutes,
        IEnumerable<DateTime> openTimes,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        var openTimeList = openTimes.Distinct().ToList();

        return await dbContext.Candles
            .Where(c => c.Symbol == symbol && c.Source == source && c.IntervalMinutes == intervalMinutes && openTimeList.Contains(c.OpenTime))
            .Select(c => c.OpenTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<CandleEntity> candles, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

        dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
        await dbContext.Candles.AddRangeAsync(candles, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

}
