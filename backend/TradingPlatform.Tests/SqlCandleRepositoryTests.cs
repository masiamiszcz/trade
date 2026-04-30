using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;
using Xunit;

namespace TradingPlatform.Tests;

public sealed class SqlCandleRepositoryTests
{
    [Fact]
    public async Task GetAggregatedFromOneMinuteSourceAsync_ShouldAggregateFiveMinuteBucketsCorrectly()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<TradingPlatformDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var services = new ServiceCollection();
        services.AddScoped(_ => new TradingPlatformDbContext(options));

        var serviceProvider = services.BuildServiceProvider();

        using var scope = serviceProvider.CreateScope();
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();
            await dbContext.Database.EnsureCreatedAsync();

            var baseTime = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);
            dbContext.Candles.AddRange(new List<CandleEntity>
            {
                new() { Symbol = "BTCUSD", Source = "binance", IntervalMinutes = 1, OpenTime = baseTime.AddMinutes(0), CloseTime = baseTime.AddMinutes(1), Open = 100m, High = 101m, Low = 99m, Close = 101m, Volume = 1m },
                new() { Symbol = "BTCUSD", Source = "binance", IntervalMinutes = 1, OpenTime = baseTime.AddMinutes(1), CloseTime = baseTime.AddMinutes(2), Open = 101m, High = 102m, Low = 100m, Close = 102m, Volume = 1m },
                new() { Symbol = "BTCUSD", Source = "binance", IntervalMinutes = 1, OpenTime = baseTime.AddMinutes(2), CloseTime = baseTime.AddMinutes(3), Open = 102m, High = 104m, Low = 101m, Close = 103m, Volume = 1m },
                new() { Symbol = "BTCUSD", Source = "binance", IntervalMinutes = 1, OpenTime = baseTime.AddMinutes(3), CloseTime = baseTime.AddMinutes(4), Open = 103m, High = 104m, Low = 102m, Close = 104m, Volume = 1m },
                new() { Symbol = "BTCUSD", Source = "binance", IntervalMinutes = 1, OpenTime = baseTime.AddMinutes(4), CloseTime = baseTime.AddMinutes(5), Open = 104m, High = 105m, Low = 103m, Close = 105m, Volume = 1m },
                new() { Symbol = "BTCUSD", Source = "binance", IntervalMinutes = 1, OpenTime = baseTime.AddMinutes(5), CloseTime = baseTime.AddMinutes(6), Open = 105m, High = 106m, Low = 104m, Close = 106m, Volume = 1m },
            });

            await dbContext.SaveChangesAsync();
        }

        var repository = new SqlCandleRepository(serviceProvider);

        var from = new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMinutes(10);

        var result = await repository.GetAggregatedFromOneMinuteSourceAsync(
            "BTCUSD",
            "binance",
            5,
            from,
            to,
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var firstCandle = result[0];
        Assert.Equal(new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc), firstCandle.OpenTime);
        Assert.Equal(new DateTime(2026, 4, 30, 0, 5, 0, DateTimeKind.Utc), firstCandle.CloseTime);
        Assert.Equal(100m, firstCandle.Open);
        Assert.Equal(105m, firstCandle.High);
        Assert.Equal(99m, firstCandle.Low);
        Assert.Equal(105m, firstCandle.Close);
        Assert.Equal(5m, firstCandle.Volume);

        var secondCandle = result[1];
        Assert.Equal(new DateTime(2026, 4, 30, 0, 5, 0, DateTimeKind.Utc), secondCandle.OpenTime);
        Assert.Equal(new DateTime(2026, 4, 30, 0, 10, 0, DateTimeKind.Utc), secondCandle.CloseTime);
        Assert.Equal(105m, secondCandle.Open);
        Assert.Equal(106m, secondCandle.High);
        Assert.Equal(104m, secondCandle.Low);
        Assert.Equal(106m, secondCandle.Close);
        Assert.Equal(1m, secondCandle.Volume);
    }
}
