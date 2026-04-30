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
            .AsNoTracking()
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
            .AsNoTracking()
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

        var candlesQuery = dbContext.Candles
            .AsNoTracking()
            .Where(c => c.Symbol == symbol && c.Source == source && c.IntervalMinutes == 1)
            .Where(c => c.OpenTime >= from && c.OpenTime < to);

        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var candles = await candlesQuery
                .OrderBy(c => c.OpenTime)
                .ToListAsync(cancellationToken);

            return candles
                .GroupBy(c => GetIntervalBoundary(c.OpenTime, targetIntervalMinutes))
                .Select(group =>
                {
                    var ordered = group.OrderBy(c => c.OpenTime).ToList();
                    return new CandleEntity
                    {
                        Symbol = symbol,
                        Source = source,
                        IntervalMinutes = targetIntervalMinutes,
                        OpenTime = group.Key,
                        CloseTime = group.Key.AddMinutes(targetIntervalMinutes),
                        Open = ordered.First().Open,
                        High = ordered.Max(c => c.High),
                        Low = ordered.Min(c => c.Low),
                        Close = ordered.Last().Close,
                        Volume = ordered.Sum(c => c.Volume)
                    };
                })
                .OrderBy(c => c.OpenTime)
                .ToList();
        }

        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var grouped = candlesQuery
            .GroupBy(c => EF.Functions.DateDiffMinute(epoch, c.OpenTime) / targetIntervalMinutes)
            .Select(group => new
            {
                Bucket = group.Key,
                OpenTime = epoch.AddMinutes(group.Key * targetIntervalMinutes),
                CloseTime = epoch.AddMinutes((group.Key + 1) * targetIntervalMinutes),
                FirstOpenTime = group.Min(c => c.OpenTime),
                LastOpenTime = group.Max(c => c.OpenTime),
                High = group.Max(c => c.High),
                Low = group.Min(c => c.Low),
                Volume = group.Sum(c => c.Volume)
            });

        return await grouped
            .Select(group => new CandleEntity
            {
                Symbol = symbol,
                Source = source,
                IntervalMinutes = targetIntervalMinutes,
                OpenTime = group.OpenTime,
                CloseTime = group.CloseTime,
                Open = candlesQuery
                    .Where(c => EF.Functions.DateDiffMinute(epoch, c.OpenTime) / targetIntervalMinutes == group.Bucket
                                && c.OpenTime == group.FirstOpenTime)
                    .Select(c => c.Open)
                    .FirstOrDefault(),
                High = group.High,
                Low = group.Low,
                Close = candlesQuery
                    .Where(c => EF.Functions.DateDiffMinute(epoch, c.OpenTime) / targetIntervalMinutes == group.Bucket
                                && c.OpenTime == group.LastOpenTime)
                    .Select(c => c.Close)
                    .FirstOrDefault(),
                Volume = group.Volume
            })
            .OrderBy(c => c.OpenTime)
            .ToListAsync(cancellationToken);
    }

    private static DateTime GetIntervalBoundary(DateTime openTime, int intervalMinutes)
    {
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var totalMinutes = (int)((openTime.ToUniversalTime() - epoch).TotalMinutes);
        var bucket = totalMinutes / intervalMinutes;
        return epoch.AddMinutes(bucket * intervalMinutes);
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
