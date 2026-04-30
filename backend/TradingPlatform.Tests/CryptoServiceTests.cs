using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;
using TradingPlatform.Data.Services;
using Xunit;

namespace TradingPlatform.Tests;

public sealed class CryptoServiceTests
{
    [Fact]
    public async Task GetChartCandlesAsync_WhenSourceIntervalIsSmallerThanDesired_UsesSqlAggregation()
    {
        var instrument = new InstrumentDto(
            Guid.NewGuid(),
            "BTCUSD",
            "Bitcoin",
            "Bitcoin USD",
            InstrumentType.Crypto.ToString(),
            "Crypto",
            "USD",
            "USD",
            "Approved",
            false,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(x => x.GetBySymbolAsync("BTCUSD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var candleRepository = new Mock<ICandleRepository>();

        candleRepository
            .Setup(x => x.GetBySymbolSourceIntervalAsync(
                "BTCUSD",
                "binance",
                1,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity>
            {
                new()
                {
                    Symbol = "BTCUSD",
                    Source = "binance",
                    IntervalMinutes = 30,
                    OpenTime = DateTime.UtcNow.AddMinutes(-90),
                    CloseTime = DateTime.UtcNow.AddMinutes(-60),
                    Open = 27000m,
                    High = 27200m,
                    Low = 26800m,
                    Close = 27100m,
                    Volume = 1.2m
                }
            });

        candleRepository
            .Setup(x => x.GetAggregatedFromOneMinuteSourceAsync(
                "BTCUSD",
                "binance",
                60,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity>
            {
                new()
                {
                    Symbol = "BTCUSD",
                    Source = "binance",
                    IntervalMinutes = 60,
                    OpenTime = DateTime.UtcNow.AddMinutes(-60),
                    CloseTime = DateTime.UtcNow,
                    Open = 27000m,
                    High = 27200m,
                    Low = 26800m,
                    Close = 27100m,
                    Volume = 2.4m
                }
            });

        var service = new CryptoService(
            instrumentService.Object,
            candleRepository.Object);

        var result = (await service.GetChartCandlesAsync("btcUsd", 50000, null, CancellationToken.None)).ToList();

        Assert.Single(result);
        Assert.Equal("BTCUSD", result[0].Symbol);
        Assert.Equal("1h", result[0].Interval);

        candleRepository.Verify(x => x.GetAggregatedFromOneMinuteSourceAsync(
            "BTCUSD",
            "binance",
            60,
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetChartCandlesAsync_WhenDesiredIntervalExists_DoesNotUseSqlAggregation()
    {
        var instrument = new InstrumentDto(
            Guid.NewGuid(),
            "BTCUSD",
            "Bitcoin",
            "Bitcoin USD",
            InstrumentType.Crypto.ToString(),
            "Crypto",
            "USD",
            "USD",
            "Approved",
            false,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(x => x.GetBySymbolAsync("BTCUSD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(instrument);

        var candleRepository = new Mock<ICandleRepository>();

        candleRepository
            .Setup(x => x.GetBySymbolSourceIntervalAsync(
                "BTCUSD",
                "binance",
                1,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity>
            {
                new()
                {
                    Symbol = "BTCUSD",
                    Source = "binance",
                    IntervalMinutes = 1,
                    OpenTime = DateTime.UtcNow.AddMinutes(-2),
                    CloseTime = DateTime.UtcNow.AddMinutes(-1),
                    Open = 27000m,
                    High = 27050m,
                    Low = 26950m,
                    Close = 27010m,
                    Volume = 0.5m
                },
                new()
                {
                    Symbol = "BTCUSD",
                    Source = "binance",
                    IntervalMinutes = 1,
                    OpenTime = DateTime.UtcNow.AddMinutes(-1),
                    CloseTime = DateTime.UtcNow,
                    Open = 27010m,
                    High = 27080m,
                    Low = 27000m,
                    Close = 27060m,
                    Volume = 0.6m
                }
            });

        var service = new CryptoService(
            instrumentService.Object,
            candleRepository.Object);

        var result = (await service.GetChartCandlesAsync("BTCUSD", 2, null, CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, candle => Assert.Equal("1m", candle.Interval));

        candleRepository.Verify(x => x.GetAggregatedFromOneMinuteSourceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
