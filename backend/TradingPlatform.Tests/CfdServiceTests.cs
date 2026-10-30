using Moq;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Entities;
using TradingPlatform.Data.Repositories;
using TradingPlatform.Data.Services;

namespace TradingPlatform.Tests;

public sealed class CfdServiceTests
{
    [Fact]
    public async Task GetAvailableCfdInstrumentsAsync_ReturnsCfdWithoutExcludingSameSymbolCrypto()
    {
        var cfd = CreateInstrument("BTCUSDT", InstrumentType.Cfd.ToString());
        var crypto = CreateInstrument("BTCUSDT", InstrumentType.Crypto.ToString());
        var cfdWithoutSource = CreateInstrument("US500CFD", InstrumentType.Cfd.ToString());
        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(service => service.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { crypto, cfd, cfdWithoutSource });
        var candleRepository = new Mock<ICandleRepository>();
        var latestCandle = CreateCandle("BTCUSDT", 1);
        latestCandle.OpenTime = DateTime.SpecifyKind(latestCandle.OpenTime, DateTimeKind.Unspecified);
        candleRepository
            .Setup(repository => repository.GetBySymbolSourceIntervalAsync(
                "BTCUSDT", "binance", 1, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity> { latestCandle });
        candleRepository
            .Setup(repository => repository.GetAggregatedFromOneMinuteSourceAsync(
                "BTCUSDT", "binance", 240, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity> { CreateCandle("BTCUSDT", 240) });

        var service = new CfdService(instrumentService.Object, candleRepository.Object);

        var result = (await service.GetAvailableCfdInstrumentsAsync()).ToList();

        var summary = Assert.Single(result);
        Assert.Equal("BTCUSDT", summary.Instrument.Symbol);
        Assert.Equal(InstrumentType.Cfd.ToString(), summary.Instrument.Type);
        Assert.Equal(100m, summary.CurrentPrice);
        Assert.Equal(95m, summary.Low1h);
        Assert.Equal(110m, summary.High1h);
        Assert.Equal(0m, summary.ChangePercent1h);
        Assert.Equal(new[] { 105m }, summary.Sparkline7d);
        Assert.Equal(DateTimeKind.Utc, summary.CandleOpenTimeUtc?.Kind);
        Assert.Equal(
            DateTime.SpecifyKind(latestCandle.OpenTime, DateTimeKind.Utc),
            summary.CandleOpenTimeUtc!.Value);
    }

    [Fact]
    public async Task GetChartCandlesAsync_WhenCfdHasNoConfiguredSource_ThrowsNotFound()
    {
        var cfd = CreateInstrument("US500CFD", InstrumentType.Cfd.ToString());
        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(service => service.GetBySymbolAsync(
                "US500CFD",
                InstrumentType.Cfd,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cfd);

        var candleRepository = new Mock<ICandleRepository>();
        var service = new CfdService(instrumentService.Object, candleRepository.Object);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetChartCandlesAsync("US500CFD", 60));

        Assert.Contains("No market data source is configured", exception.Message);
        candleRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetChartCandlesAsync_UsesSameSymbolForCfdAndReturnsRequestedInterval()
    {
        var cfd = CreateInstrument("BTCUSDT", InstrumentType.Cfd.ToString());
        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(service => service.GetBySymbolAsync(
                "BTCUSDT",
                InstrumentType.Cfd,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cfd);

        var sourceCandle = CreateCandle("BTCUSDT", 1);
        var candleRepository = new Mock<ICandleRepository>();
        candleRepository
            .Setup(repository => repository.GetBySymbolSourceIntervalAsync(
                "BTCUSDT",
                "binance",
                1,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity> { sourceCandle });

        var service = new CfdService(instrumentService.Object, candleRepository.Object);

        var result = (await service.GetChartCandlesAsync("btcusdt", 60, 1)).ToList();

        var candle = Assert.Single(result);
        Assert.Equal("BTCUSDT", candle.Symbol);
        Assert.Equal("1m", candle.Interval);
        candleRepository.VerifyAll();
    }

    [Fact]
    public async Task GetChartCandlesAsync_WithFiveMinuteInterval_AggregatesSameSourceSymbol()
    {
        var cfd = CreateInstrument("BTCUSDT", InstrumentType.Cfd.ToString());
        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(service => service.GetBySymbolAsync(
                "BTCUSDT",
                InstrumentType.Cfd,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cfd);

        var candleRepository = new Mock<ICandleRepository>();
        candleRepository
            .Setup(repository => repository.GetAggregatedFromOneMinuteSourceAsync(
                "BTCUSDT",
                "binance",
                5,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CandleEntity> { CreateCandle("BTCUSDT", 5) });

        var service = new CfdService(instrumentService.Object, candleRepository.Object);

        var result = (await service.GetChartCandlesAsync("BTCUSDT", 60, 5)).ToList();

        Assert.Equal("5m", Assert.Single(result).Interval);
        candleRepository.VerifyAll();
    }

    [Fact]
    public async Task GetChartCandlesAsync_WhenSymbolExistsOnlyAsCrypto_ThrowsNotFound()
    {
        var instrumentService = new Mock<IInstrumentService>();
        instrumentService
            .Setup(service => service.GetBySymbolAsync(
                "BTCUSDT",
                InstrumentType.Cfd,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("CFD not found"));

        var service = new CfdService(instrumentService.Object, new Mock<ICandleRepository>().Object);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetChartCandlesAsync("BTCUSDT", 60));
    }

    private static InstrumentDto CreateInstrument(string symbol, string type)
        => new(
            Guid.NewGuid(),
            symbol,
            symbol,
            symbol,
            type,
            type == InstrumentType.Cfd.ToString() ? "Cfd" : "Crypto",
            "USD",
            "USDT",
            InstrumentStatus.Approved.ToString(),
            false,
            Guid.Empty,
            DateTimeOffset.UtcNow);

    private static CandleEntity CreateCandle(string symbol, int intervalMinutes)
        => new()
        {
            Symbol = symbol,
            Source = "binance",
            IntervalMinutes = intervalMinutes,
            OpenTime = intervalMinutes == 240
                ? DateTime.UtcNow.AddHours(-4)
                : DateTime.UtcNow.AddMinutes(-1),
            CloseTime = intervalMinutes == 240 ? DateTime.UtcNow : DateTime.UtcNow.AddSeconds(-1),
            Open = intervalMinutes == 240 ? 90m : 100m,
            High = 110m,
            Low = 95m,
            Close = intervalMinutes == 240 ? 105m : 100m,
            Volume = 2m
        };
}
